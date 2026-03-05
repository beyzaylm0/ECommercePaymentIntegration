using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ECommercePaymentIntegration.Application.DTOs.Requests;
using ECommercePaymentIntegration.Application.DTOs.Responses;
using ECommercePaymentIntegration.Application.ExternalServices;
using Shouldly;
using Moq;
using Xunit;

namespace ECommercePaymentIntegration.IntegrationTests.Controllers;

public class ProductsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProductsControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProducts_ShouldReturn200WithProducts()
    {
        var response = await _client.GetAsync("/api/products");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldContain("Premium Smartphone");
        content.ShouldContain("Wireless Headphones");
        content.ShouldContain("Laptop");
    }

    [Fact]
    public async Task GetProducts_ShouldReturnCorrectStructure()
    {
        var response = await _client.GetAsync("/api/products");
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<List<ProductResponse>>>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        content.ShouldNotBeNull();
        content!.Success.ShouldBeTrue();
        content.Data!.Count.ShouldBe(5);
        content.Data[0].Price.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task GetProducts_ShouldReturnJsonContentType()
    {
        var response = await _client.GetAsync("/api/products");

        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
    }
}

public class OrdersControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public OrdersControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region CreateOrder Tests

    [Fact]
    public async Task CreateOrder_WithValidRequest_ShouldReturn201()
    {
        var request = new CreateOrderRequest
        {
            Items = new List<OrderItemRequest>
            {
                new() { ProductId = "prod-001", Quantity = 1 },
                new() { ProductId = "prod-002", Quantity = 2 }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/orders/create", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldContain("Reserved");
    }

    [Fact]
    public async Task CreateOrder_WithEmptyItems_ShouldReturn400()
    {
        var request = new CreateOrderRequest { Items = new List<OrderItemRequest>() };

        var response = await _client.PostAsJsonAsync("/api/orders/create", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_WithInvalidQuantity_ShouldReturn400()
    {
        var request = new CreateOrderRequest
        {
            Items = new List<OrderItemRequest>
            {
                new() { ProductId = "prod-001", Quantity = 0 }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/orders/create", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_WithNonExistentProduct_ShouldReturn404()
    {
        var request = new CreateOrderRequest
        {
            Items = new List<OrderItemRequest>
            {
                new() { ProductId = "non-existent", Quantity = 1 }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/orders/create", request);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateOrder_WithInsufficientStock_ShouldReturn400()
    {
        var request = new CreateOrderRequest
        {
            Items = new List<OrderItemRequest>
            {
                new() { ProductId = "prod-003", Quantity = 1 } // Stock is 0
            }
        };

        var response = await _client.PostAsJsonAsync("/api/orders/create", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_WithDuplicateProductIds_ShouldReturn400()
    {
        var request = new CreateOrderRequest
        {
            Items = new List<OrderItemRequest>
            {
                new() { ProductId = "prod-001", Quantity = 1 },
                new() { ProductId = "prod-001", Quantity = 2 }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/orders/create", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldContain("Duplicate");
    }

    [Fact]
    public async Task CreateOrder_WithNegativeQuantity_ShouldReturn400()
    {
        var request = new CreateOrderRequest
        {
            Items = new List<OrderItemRequest>
            {
                new() { ProductId = "prod-001", Quantity = -1 }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/orders/create", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_ShouldReturnOrderWithCorrectTotalAmount()
    {
        var request = new CreateOrderRequest
        {
            Items = new List<OrderItemRequest>
            {
                new() { ProductId = "prod-002", Quantity = 3 }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/orders/create", request);
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<OrderResponse>>(JsonOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        content!.Data!.TotalAmount.ShouldBe(44.97m); // 14.99 * 3
    }

    #endregion

    #region CompleteOrder Tests

    [Fact]
    public async Task CompleteOrder_WithNonExistentOrder_ShouldReturn404()
    {
        var response = await _client.PostAsync("/api/orders/non-existent/complete", null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task FullOrderFlow_CreateAndComplete_ShouldSucceed()
    {
        var createRequest = new CreateOrderRequest
        {
            Items = new List<OrderItemRequest>
            {
                new() { ProductId = "prod-002", Quantity = 1 }
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/orders/create", createRequest);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createResult = JsonSerializer.Deserialize<ApiResponse<OrderResponse>>(createContent, JsonOptions);
        var orderId = createResult?.Data?.Id;
        orderId.ShouldNotBeNullOrEmpty();

        var completeResponse = await _client.PostAsync($"/api/orders/{orderId}/complete", null);

        completeResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var completeContent = await completeResponse.Content.ReadAsStringAsync();
        completeContent.ShouldContain("Completed");
    }

    #endregion

    #region CancelOrder Tests

    [Fact]
    public async Task CancelOrder_WithNonExistentOrder_ShouldReturn404()
    {
        var response = await _client.PostAsync("/api/orders/non-existent/cancel", null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task FullOrderFlow_CreateAndCancel_ShouldSucceed()
    {
        var createRequest = new CreateOrderRequest
        {
            Items = new List<OrderItemRequest>
            {
                new() { ProductId = "prod-002", Quantity = 1 }
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/orders/create", createRequest);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var createContent = await createResponse.Content.ReadFromJsonAsync<ApiResponse<OrderResponse>>(JsonOptions);
        var orderId = createContent?.Data?.Id;

        var cancelResponse = await _client.PostAsync($"/api/orders/{orderId}/cancel", null);

        cancelResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var cancelContent = await cancelResponse.Content.ReadAsStringAsync();
        cancelContent.ShouldContain("Cancelled");
    }

    [Fact]
    public async Task CompleteOrder_AfterCancel_ShouldReturn400()
    {
        var createRequest = new CreateOrderRequest
        {
            Items = new List<OrderItemRequest>
            {
                new() { ProductId = "prod-002", Quantity = 1 }
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/orders/create", createRequest);
        var createContent = await createResponse.Content.ReadFromJsonAsync<ApiResponse<OrderResponse>>(JsonOptions);
        var orderId = createContent?.Data?.Id;

        await _client.PostAsync($"/api/orders/{orderId}/cancel", null);

        var completeResponse = await _client.PostAsync($"/api/orders/{orderId}/complete", null);

        completeResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    #endregion

    #region GetOrder Tests

    [Fact]
    public async Task GetOrder_AfterCreation_ShouldReturnOrder()
    {
        var createRequest = new CreateOrderRequest
        {
            Items = new List<OrderItemRequest>
            {
                new() { ProductId = "prod-001", Quantity = 1 }
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/orders/create", createRequest);
        var createContent = await createResponse.Content.ReadFromJsonAsync<ApiResponse<OrderResponse>>(JsonOptions);
        var orderId = createContent?.Data?.Id;

        var getResponse = await _client.GetAsync($"/api/orders/{orderId}");

        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var getContent = await getResponse.Content.ReadAsStringAsync();
        getContent.ShouldContain(orderId!);
    }

    [Fact]
    public async Task GetOrder_WithNonExistentId_ShouldReturn404()
    {
        var response = await _client.GetAsync("/api/orders/non-existent-id");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region GetAllOrders Tests

    [Fact]
    public async Task GetAllOrders_ShouldReturn200()
    {
        var response = await _client.GetAsync("/api/orders");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    #endregion

    #region Health Check Tests

    [Fact]
    public async Task HealthCheck_ShouldReturnSuccessStatusCode()
    {
        var response = await _client.GetAsync("/health");

        response.IsSuccessStatusCode.ShouldBeTrue();
    }

    [Fact]
    public async Task HealthCheck_ShouldReturnHealthyInBody()
    {
        var response = await _client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();

        body.ShouldBe("Healthy");
    }

    #endregion

    #region Security Headers Tests

    [Fact]
    public async Task Response_ShouldContainSecurityHeaders()
    {
        var response = await _client.GetAsync("/api/products");

        response.Headers.Contains("X-Content-Type-Options").ShouldBeTrue();
        response.Headers.GetValues("X-Content-Type-Options").ShouldContain("nosniff");
    }

    [Fact]
    public async Task Response_ShouldContainCorrelationId()
    {
        var response = await _client.GetAsync("/api/products");

        response.Headers.Contains("X-Correlation-Id").ShouldBeTrue();
    }

    #endregion
}
