using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using OrderService.Application.Requests;
using OrderService.Tests.Integration.Infrastructure;
using OrderService.Tests.Integration.Models;

namespace OrderService.Tests.Integration.Scenarios;

[Collection(IntegrationTestCollection.Name)]
public class OrderServiceApiIntegrationTests(IntegrationTestFixture fixture)
{
    private const string AuthTokenRoute = "/auth/token";
    private const string OrdersRoute = "/orders";
    private const string ProductsRoute = "/products";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IntegrationTestFixture _fixture = fixture;

    [Fact]
    public async Task AnonymousUsers_CanGetToken_ButCannotAccessProtectedEndpoints()
    {
        await _fixture.ResetDatabaseAsync();

        using var client = _fixture.CreateClient();

        var tokenResponse = await client.PostAsJsonAsync(AuthTokenRoute, new
        {
            email = "anonymous@test.local",
            password = "unused"
        });

        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var tokenPayload = await ReadAsAsync<TokenResponse>(tokenResponse);
        tokenPayload.Token.Should().NotBeNullOrWhiteSpace();
        tokenPayload.ExpiresIn.Should().Be(3600);

        var ordersResponse = await client.GetAsync(OrdersRoute);
        ordersResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var productsResponse = await client.GetAsync(ProductsRoute);
        productsResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AuthenticatedEndpoints_ShouldRejectInvalidTokens()
    {
        await _fixture.ResetDatabaseAsync();

        using var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

        var ordersResponse = await client.GetAsync(OrdersRoute);
        ordersResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var productsResponse = await client.GetAsync(ProductsRoute);
        productsResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task HappyPath_ShouldCreateConfirmAndCancelOrder_WithExpectedStockTransitions()
    {
        await _fixture.ResetDatabaseAsync();

        using var client = _fixture.CreateClient();
        var token = await GetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var productName = $"Integration Product {Guid.NewGuid():N}";
        var createProductResponse = await client.PostAsJsonAsync(ProductsRoute, new CreateProductRequest
        {
            Name = productName,
            UnitPrice = 19.99m,
            AvailableQuantity = 10
        });

        createProductResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdProductEnvelope = await ReadAsAsync<ApiEnvelope<ProductResponse>>(createProductResponse);
        createdProductEnvelope.Success.Should().BeTrue();
        createdProductEnvelope.Data.Should().NotBeNull();

        var createdProduct = createdProductEnvelope.Data!;
        createdProduct.Name.Should().Be(productName);
        createdProduct.AvailableQuantity.Should().Be(10);

        var listProductsResponse = await client.GetAsync($"{ProductsRoute}?pageNumber=1&pageSize=10");
        listProductsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var listedProducts = await ReadAsAsync<PagedResult<ProductResponse>>(listProductsResponse);
        listedProducts.Items.Should().ContainSingle(product => product.Id == createdProduct.Id);

        var insufficientOrderResponse = await client.PostAsJsonAsync(OrdersRoute, new CreateOrderRequest
        {
            CustomerId = Guid.NewGuid(),
            Currency = "USD",
            Items =
            [
                new CreateOrderItemRequest
                {
                    ProductId = createdProduct.Id,
                    Quantity = 15
                }
            ]
        });

        insufficientOrderResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var insufficientStock = await ReadAsAsync<ErrorResponse>(insufficientOrderResponse);
        insufficientStock.Errors.Should().NotBeNullOrEmpty();
        insufficientStock.Errors!.Single().Should().Contain("Insufficient stock");

        var createOrderResponse = await client.PostAsJsonAsync(OrdersRoute, new CreateOrderRequest
        {
            CustomerId = Guid.NewGuid(),
            Currency = "USD",
            Items =
            [
                new CreateOrderItemRequest
                {
                    ProductId = createdProduct.Id,
                    Quantity = 5
                }
            ]
        });

        createOrderResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdOrder = await ReadAsAsync<OrderResponse>(createOrderResponse);
        createdOrder.Status.Should().Be("Placed");
        createdOrder.Items.Should().ContainSingle(item => item.ProductId == createdProduct.Id && item.Quantity == 5);

        var listOrdersResponse = await client.GetAsync($"{OrdersRoute}?pageNumber=1&pageSize=10");
        listOrdersResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var listedOrders = await ReadAsAsync<PagedResult<OrderListItemResponse>>(listOrdersResponse);
        listedOrders.Items.Should().ContainSingle(order => order.Id == createdOrder.Id && order.Status == "Placed");

        var orderByIdResponse = await client.GetAsync($"/orders/{createdOrder.Id}");
        orderByIdResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var placedOrder = await ReadAsAsync<OrderResponse>(orderByIdResponse);
        placedOrder.Status.Should().Be("Placed");

        var confirmResponse = await client.PostAsync($"/orders/{createdOrder.Id}/confirm", content: null);
        confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var confirmedOrder = await ReadAsAsync<OrderResponse>(confirmResponse);
        confirmedOrder.Status.Should().Be("Confirmed");

        var productAfterConfirmResponse = await client.GetAsync($"/products/{createdProduct.Id}");
        productAfterConfirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var productAfterConfirm = await ReadAsAsync<ProductResponse>(productAfterConfirmResponse);
        productAfterConfirm.AvailableQuantity.Should().Be(5);

        var confirmedOrderByIdResponse = await client.GetAsync($"/orders/{createdOrder.Id}");
        confirmedOrderByIdResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var confirmedOrderById = await ReadAsAsync<OrderResponse>(confirmedOrderByIdResponse);
        confirmedOrderById.Status.Should().Be("Confirmed");

        var cancelResponse = await client.PostAsync($"/orders/{createdOrder.Id}/cancel", content: null);
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var canceledOrder = await ReadAsAsync<OrderResponse>(cancelResponse);
        canceledOrder.Status.Should().Be("Canceled");

        var canceledOrderByIdResponse = await client.GetAsync($"/orders/{createdOrder.Id}");
        canceledOrderByIdResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var canceledOrderById = await ReadAsAsync<OrderResponse>(canceledOrderByIdResponse);
        canceledOrderById.Status.Should().Be("Canceled");

        var productAfterCancelResponse = await client.GetAsync($"/products/{createdProduct.Id}");
        productAfterCancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var productAfterCancel = await ReadAsAsync<ProductResponse>(productAfterCancelResponse);
        productAfterCancel.AvailableQuantity.Should().Be(10);
    }

    private static async Task<string> GetTokenAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(AuthTokenRoute, new
        {
            email = "integration@test.local",
            password = "unused"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var tokenPayload = await ReadAsAsync<TokenResponse>(response);
        tokenPayload.Token.Should().NotBeNullOrWhiteSpace();

        return tokenPayload.Token;
    }

    private static async Task<T> ReadAsAsync<T>(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        payload.Should().NotBeNull();
        return payload!;
    }
}
