using Microsoft.Extensions.Options;
using OrderService.Domain.Entities;
using OrderService.Domain.Repositories;

namespace OrderService.Infrastructure.Idempotency;

public class IdempotencyService(
    IIdempotencyCache cache,
    IOptions<IdempotencyOptions> options,
    IUnitOfWork unitOfWork) : IIdempotencyService
{
    private const string InProgressMessage = "A request with the same Idempotency-Key is already being processed.";
    private const string MismatchedRequestMessage = "The supplied Idempotency-Key was already used with a different request.";

    private readonly IIdempotencyCache _cache = cache;
    private readonly IdempotencyOptions _options = options.Value;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<IdempotencyBeginResult> BeginRequestAsync(IdempotencyRequest request, CancellationToken cancellationToken = default)
    {
        var existingRecord = await _unitOfWork.IdempotencyRecords.GetByKeyAsync(request.IdempotencyKey, cancellationToken);
        if (existingRecord is not null)
        {
            var earlyResult = await TryHandleExistingRecordAsync(existingRecord, request, cancellationToken);
            if (earlyResult is not null)
            {
                return earlyResult;
            }
        }

        var lockAcquired = await _cache.TryAcquireLockAsync(request.IdempotencyKey, TimeSpan.FromSeconds(_options.LockTtlSeconds), cancellationToken);
        if (!lockAcquired)
        {
            return await HandleLockUnavailableAsync(request, cancellationToken);
        }

        try
        {
            existingRecord = await _unitOfWork.IdempotencyRecords.GetByKeyAsync(request.IdempotencyKey, cancellationToken);
            var creationResult = await CreateOrResetPendingAsync(existingRecord, request, cancellationToken);
            if (creationResult is not null)
            {
                return creationResult;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return IdempotencyBeginResult.Proceed();
        }
        catch
        {
            await _cache.ReleaseLockAsync(request.IdempotencyKey, cancellationToken);
            throw;
        }
    }

    public async Task CompleteRequestAsync(IdempotencyRequest request, IdempotencyStoredResponse response, CancellationToken cancellationToken = default)
    {
        var existingRecord = await _unitOfWork.IdempotencyRecords.GetByKeyAsync(request.IdempotencyKey, cancellationToken);
        if (existingRecord is null || !existingRecord.Matches(request.Method, request.Endpoint, request.RequestHash))
        {
            await _cache.ReleaseLockAsync(request.IdempotencyKey, cancellationToken);
            return;
        }

        existingRecord.Complete(
            response.StatusCode,
            response.Body,
            response.ContentType,
            response.Headers.Count == 0 ? null : System.Text.Json.JsonSerializer.Serialize(response.Headers));

        await _unitOfWork.IdempotencyRecords.UpdateAsync(existingRecord, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _cache.SetResponseAsync(request.IdempotencyKey, response, TimeSpan.FromMinutes(_options.ResponseTtlMinutes), cancellationToken);
        await _cache.ReleaseLockAsync(request.IdempotencyKey, cancellationToken);
    }

    public async Task FailRequestAsync(IdempotencyRequest request, CancellationToken cancellationToken = default)
    {
        var existingRecord = await _unitOfWork.IdempotencyRecords.GetByKeyAsync(request.IdempotencyKey, cancellationToken);
        if (existingRecord is not null && existingRecord.Matches(request.Method, request.Endpoint, request.RequestHash))
        {
            existingRecord.MarkFailed();
            await _unitOfWork.IdempotencyRecords.UpdateAsync(existingRecord, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        await _cache.ReleaseLockAsync(request.IdempotencyKey, cancellationToken);
    }

    private async Task<IdempotencyBeginResult?> CreateOrResetPendingAsync(
        IdempotencyRecord? existingRecord,
        IdempotencyRequest request,
        CancellationToken cancellationToken)
    {
        if (existingRecord is null)
        {
            var pendingRecord = IdempotencyRecord.CreatePending(
                request.IdempotencyKey,
                request.Method,
                request.Endpoint,
                request.RequestHash,
                DateTime.UtcNow.AddMinutes(_options.ResponseTtlMinutes));

            await _unitOfWork.IdempotencyRecords.AddAsync(pendingRecord, cancellationToken);
            return null;
        }

        if (!existingRecord.Matches(request.Method, request.Endpoint, request.RequestHash))
        {
            await _cache.ReleaseLockAsync(request.IdempotencyKey, cancellationToken);
            return IdempotencyBeginResult.Conflict(MismatchedRequestMessage);
        }

        if (existingRecord.IsCompleted)
        {
            var storedResponse = ToStoredResponse(existingRecord);
            if (storedResponse is not null)
            {
                await _cache.SetResponseAsync(request.IdempotencyKey, storedResponse, TimeSpan.FromMinutes(_options.ResponseTtlMinutes), cancellationToken);
                await _cache.ReleaseLockAsync(request.IdempotencyKey, cancellationToken);
                return IdempotencyBeginResult.Replay(storedResponse);
            }
        }

        if (existingRecord.IsInProgress)
        {
            await _cache.ReleaseLockAsync(request.IdempotencyKey, cancellationToken);
            return IdempotencyBeginResult.InProgress(InProgressMessage);
        }

        existingRecord.ResetPending(DateTime.UtcNow.AddMinutes(_options.ResponseTtlMinutes));
        await _unitOfWork.IdempotencyRecords.UpdateAsync(existingRecord, cancellationToken);
        return null;
    }

    private async Task<IdempotencyBeginResult> HandleLockUnavailableAsync(IdempotencyRequest request, CancellationToken cancellationToken)
    {
        var cachedResponse = await _cache.GetResponseAsync(request.IdempotencyKey, cancellationToken);
        if (cachedResponse is not null)
        {
            return IdempotencyBeginResult.Replay(cachedResponse);
        }

        var lockedRecord = await _unitOfWork.IdempotencyRecords.GetByKeyAsync(request.IdempotencyKey, cancellationToken);
        if (lockedRecord is not null)
        {
            var lockedResult = await TryHandleExistingRecordAsync(lockedRecord, request, cancellationToken);
            if (lockedResult is not null)
            {
                return lockedResult;
            }
        }

        return IdempotencyBeginResult.InProgress(InProgressMessage);
    }

    private async Task<IdempotencyBeginResult?> TryHandleExistingRecordAsync(
        IdempotencyRecord existingRecord,
        IdempotencyRequest request,
        CancellationToken cancellationToken)
    {
        if (!existingRecord.Matches(request.Method, request.Endpoint, request.RequestHash))
        {
            return IdempotencyBeginResult.Conflict(MismatchedRequestMessage);
        }

        if (existingRecord.IsCompleted)
        {
            var response = ToStoredResponse(existingRecord);
            if (response is not null)
            {
                await _cache.SetResponseAsync(request.IdempotencyKey, response, TimeSpan.FromMinutes(_options.ResponseTtlMinutes), cancellationToken);
                return IdempotencyBeginResult.Replay(response);
            }
        }

        if (existingRecord.IsInProgress)
        {
            return IdempotencyBeginResult.InProgress(InProgressMessage);
        }

        return null;
    }

    private static IdempotencyStoredResponse? ToStoredResponse(IdempotencyRecord record)
    {
        if (!record.StatusCode.HasValue || record.ResponseBody is null)
        {
            return null;
        }

        var headers = record.ResponseHeaders is null
            ? new Dictionary<string, string[]>()
            : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string[]>>(record.ResponseHeaders) ?? [];

        return new IdempotencyStoredResponse
        {
            Body = record.ResponseBody,
            ContentType = record.ContentType,
            Headers = headers,
            StatusCode = record.StatusCode.Value
        };
    }
}
