using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Extensions;
using OrderService.Infrastructure.Idempotency;

namespace OrderService.Api.Middleware;

public class IdempotencyMiddleware(RequestDelegate next)
{
    private const string IdempotencyHeader = "Idempotency-Key";
    private static readonly HashSet<string> IgnoredResponseHeaders =
    [
        "Date",
            "Server",
            "Transfer-Encoding"
    ];

    private readonly RequestDelegate _next = next;

    public async Task Invoke(HttpContext context, IIdempotencyService idempotencyService)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<IdempotentRequestAttribute>() is null)
        {
            await _next(context);
            return;
        }

        var idempotencyKey = context.Request.Headers[IdempotencyHeader].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = $"Missing {IdempotencyHeader} header." });
            return;
        }

        if (!Guid.TryParse(idempotencyKey, out _))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = $"{IdempotencyHeader} must be a valid UUID." });
            return;
        }

        var request = await CreateRequestAsync(context, idempotencyKey);
        var beginResult = await idempotencyService.BeginRequestAsync(request, context.RequestAborted);
        if (beginResult.Status == IdempotencyBeginStatus.Replay && beginResult.Response is not null)
        {
            await WriteStoredResponseAsync(context, beginResult.Response);
            return;
        }

        if (beginResult.Status == IdempotencyBeginStatus.Conflict)
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await context.Response.WriteAsJsonAsync(new { error = beginResult.Message });
            return;
        }

        if (beginResult.Status == IdempotencyBeginStatus.InProgress)
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await context.Response.WriteAsJsonAsync(new { error = beginResult.Message });
            return;
        }

        var originalBody = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await _next(context);

            buffer.Position = 0;
            var responseBody = await new StreamReader(buffer).ReadToEndAsync(context.RequestAborted);
            buffer.Position = 0;

            if (context.Response.StatusCode >= StatusCodes.Status500InternalServerError)
            {
                await idempotencyService.FailRequestAsync(request, context.RequestAborted);
            }
            else
            {
                var storedResponse = new IdempotencyStoredResponse
                {
                    Body = responseBody,
                    ContentType = context.Response.ContentType,
                    Headers = CaptureHeaders(context.Response.Headers),
                    StatusCode = context.Response.StatusCode
                };

                await idempotencyService.CompleteRequestAsync(request, storedResponse, context.RequestAborted);
            }

            buffer.Position = 0;
            await buffer.CopyToAsync(originalBody, context.RequestAborted);
        }
        catch
        {
            await idempotencyService.FailRequestAsync(request, context.RequestAborted);
            throw;
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }

    private static Dictionary<string, string[]> CaptureHeaders(IHeaderDictionary headers)
    {
        return headers
            .Where(header => !IgnoredResponseHeaders.Contains(header.Key))
            .ToDictionary(
                header => header.Key,
                header => header.Value.Where(value => value is not null).Select(value => value!).ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    private static async Task<IdempotencyRequest> CreateRequestAsync(HttpContext context, string idempotencyKey)
    {
        context.Request.EnableBuffering();

        string requestBody;
        using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true))
        {
            requestBody = await reader.ReadToEndAsync(context.RequestAborted);
        }

        context.Request.Body.Position = 0;

        var requestIdentity = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue(ClaimTypes.Email)
            ?? "anonymous";

        var canonicalPayload = JsonSerializer.Serialize(new
        {
            identity = requestIdentity,
            method = context.Request.Method,
            path = context.Request.Path.ToString(),
            query = context.Request.QueryString.Value ?? string.Empty,
            body = requestBody
        });

        return new IdempotencyRequest
        {
            Endpoint = context.Request.GetEncodedPathAndQuery(),
            IdempotencyKey = idempotencyKey,
            Method = context.Request.Method,
            RequestHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalPayload)))
        };
    }

    private static async Task WriteStoredResponseAsync(HttpContext context, IdempotencyStoredResponse response)
    {
        context.Response.StatusCode = response.StatusCode;
        if (!string.IsNullOrWhiteSpace(response.ContentType))
        {
            context.Response.ContentType = response.ContentType;
        }

        foreach (var header in response.Headers)
        {
            if (string.Equals(header.Key, "Content-Length", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            context.Response.Headers[header.Key] = header.Value;
        }

        await context.Response.WriteAsync(response.Body);
    }
}
