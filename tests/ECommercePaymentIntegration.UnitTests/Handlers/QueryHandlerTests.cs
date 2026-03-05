using ECommercePaymentIntegration.Application.DTOs.ExternalServices;
using ECommercePaymentIntegration.Application.Exceptions;
using ECommercePaymentIntegration.Application.ExternalServices;
using ECommercePaymentIntegration.Application.Orders.Queries.GetAllOrders;
using ECommercePaymentIntegration.Application.Orders.Queries.GetOrder;
using ECommercePaymentIntegration.Application.Products.Queries.GetAllProducts;
using ECommercePaymentIntegration.Domain.Entities;
using ECommercePaymentIntegration.Domain.Enums;
using ECommercePaymentIntegration.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace ECommercePaymentIntegration.UnitTests.Handlers;

public class GetOrderQueryHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepoMock = new();
    private readonly GetOrderQueryHandler _sut;

    public GetOrderQueryHandlerTests()
    {
        _sut = new GetOrderQueryHandler(_orderRepoMock.Object);
    }

    [Fact]
    public async Task Handle_WithExistingOrder_ShouldReturnOrder()
    {
        var order = new Order
        {
            Id = "order-123",
            Status = OrderStatus.Reserved,
            Items = new List<OrderItem>
            {
                new() { ProductId = "prod-001", ProductName = "Premium Smartphone", Quantity = 1, UnitPrice = 19.99m }
            }
        };
        _orderRepoMock.Setup(x => x.GetByIdAsync("order-123")).ReturnsAsync(order);

        var result = await _sut.Handle(new GetOrderQuery { OrderId = "order-123" }, CancellationToken.None);

        result.ShouldNotBeNull();
        result!.Id.ShouldBe("order-123");
    }

    [Fact]
    public async Task Handle_WithNonExistentOrder_ShouldReturnNull()
    {
        _orderRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((Order?)null);

        var result = await _sut.Handle(new GetOrderQuery { OrderId = "non-existent" }, CancellationToken.None);

        result.ShouldBeNull();
    }
}

public class GetAllOrdersQueryHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepoMock = new();
    private readonly GetAllOrdersQueryHandler _sut;

    public GetAllOrdersQueryHandlerTests()
    {
        _sut = new GetAllOrdersQueryHandler(_orderRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnAllOrders()
    {
        var orders = new List<Order>
        {
            new() { Id = "order-1", Status = OrderStatus.Reserved },
            new() { Id = "order-2", Status = OrderStatus.Completed }
        };
        _orderRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(orders);

        var result = (await _sut.Handle(new GetAllOrdersQuery(), CancellationToken.None)).ToList();

        result.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Handle_WhenEmpty_ShouldReturnEmptyList()
    {
        _orderRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(Enumerable.Empty<Order>());

        var result = await _sut.Handle(new GetAllOrdersQuery(), CancellationToken.None);

        result.ShouldBeEmpty();
    }
}

public class GetAllProductsQueryHandlerTests
{
    private readonly Mock<IBalanceManagementService> _balanceServiceMock = new();
    private readonly Mock<ILogger<GetAllProductsQueryHandler>> _loggerMock = new();
    private readonly GetAllProductsQueryHandler _sut;

    public GetAllProductsQueryHandlerTests()
    {
        _sut = new GetAllProductsQueryHandler(_balanceServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnMappedProducts()
    {
        var products = new List<ProductDto>
        {
            new("1", "Laptop", "A laptop", 999.99m, "USD", "Electronics", 10),
            new("2", "Mouse", "A mouse", 29.99m, "USD", "Accessories", 50)
        };

        _balanceServiceMock.Setup(x => x.GetProductsAsync()).ReturnsAsync(products);

        var result = (await _sut.Handle(new GetAllProductsQuery(), CancellationToken.None)).ToList();

        result.Count.ShouldBe(2);
        result[0].Id.ShouldBe("1");
        result[0].Name.ShouldBe("Laptop");
        result[0].Price.ShouldBe(999.99m);
        result[0].Currency.ShouldBe("USD");
        result[0].Category.ShouldBe("Electronics");
        result[0].Description.ShouldBe("A laptop");
        result[0].Stock.ShouldBe(10);
        result[1].Id.ShouldBe("2");
        result[1].Name.ShouldBe("Mouse");
    }

    [Fact]
    public async Task Handle_WhenServiceFails_ShouldPropagateException()
    {
        _balanceServiceMock.Setup(x => x.GetProductsAsync())
            .ThrowsAsync(new ExternalServiceException("BalanceManagement", "Service unavailable"));

        await Assert.ThrowsAsync<ExternalServiceException>(
            () => _sut.Handle(new GetAllProductsQuery(), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenNoProducts_ShouldReturnEmptyList()
    {
        _balanceServiceMock.Setup(x => x.GetProductsAsync())
            .ReturnsAsync(Enumerable.Empty<ProductDto>());

        var result = await _sut.Handle(new GetAllProductsQuery(), CancellationToken.None);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_WhenHttpRequestFails_ShouldPropagateHttpRequestException()
    {
        _balanceServiceMock.Setup(x => x.GetProductsAsync())
            .ThrowsAsync(new HttpRequestException("Network error"));

        await Assert.ThrowsAsync<HttpRequestException>(
            () => _sut.Handle(new GetAllProductsQuery(), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenTimeout_ShouldPropagateTaskCanceledException()
    {
        _balanceServiceMock.Setup(x => x.GetProductsAsync())
            .ThrowsAsync(new TaskCanceledException("Request timed out"));

        await Assert.ThrowsAsync<TaskCanceledException>(
            () => _sut.Handle(new GetAllProductsQuery(), CancellationToken.None));
    }
}
