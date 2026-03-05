using ECommercePaymentIntegration.Application.DTOs.ExternalServices;
using ECommercePaymentIntegration.Application.DTOs.Requests;
using ECommercePaymentIntegration.Application.Exceptions;
using ECommercePaymentIntegration.Application.ExternalServices;
using ECommercePaymentIntegration.Application.Orders.Commands.CreateOrder;
using ECommercePaymentIntegration.Domain.Entities;
using ECommercePaymentIntegration.Domain.Enums;
using ECommercePaymentIntegration.Domain.Exceptions;
using ECommercePaymentIntegration.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace ECommercePaymentIntegration.UnitTests.Handlers;

public class CreateOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepoMock;
    private readonly Mock<IBalanceManagementService> _balanceServiceMock;
    private readonly Mock<ILogger<CreateOrderCommandHandler>> _loggerMock;
    private readonly CreateOrderCommandHandler _sut;

    private readonly List<ProductDto> _testProducts = new()
    {
        new("prod-001", "Premium Smartphone", "Latest model with advanced features", 19.99m, "USD", "Electronics", 42),
        new("prod-002", "Wireless Headphones", "Noise-cancelling with premium sound quality", 14.99m, "USD", "Electronics", 78),
        new("prod-003", "Smart Watch", "Fitness tracking and notifications", 12.99m, "USD", "Electronics", 0),
        new("prod-004", "Laptop", "High-performance for work and gaming", 19.99m, "USD", "Electronics", 15),
        new("prod-005", "Wireless Charger", "Fast charging for compatible devices", 9.99m, "USD", "Accessories", 120)
    };

    public CreateOrderCommandHandlerTests()
    {
        _orderRepoMock = new Mock<IOrderRepository>();
        _balanceServiceMock = new Mock<IBalanceManagementService>();
        _loggerMock = new Mock<ILogger<CreateOrderCommandHandler>>();

        _balanceServiceMock.Setup(x => x.GetProductsAsync())
            .ReturnsAsync(_testProducts);

        _sut = new CreateOrderCommandHandler(
            _orderRepoMock.Object,
            _balanceServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldCreateOrderAndReserveFunds()
    {
        var command = new CreateOrderCommand
        {
            Items = new List<OrderItemRequest>
            {
                new() { ProductId = "prod-001", Quantity = 1 },
                new() { ProductId = "prod-002", Quantity = 2 }
            }
        };

        var expectedAmount = 19.99m + (14.99m * 2); // 49.97

        _balanceServiceMock.Setup(x => x.CreatePreOrderAsync(It.IsAny<string>(), expectedAmount, It.IsAny<string?>()))
            .ReturnsAsync(new PreOrderResult(true, "Funds reserved.", It.IsAny<string>(), expectedAmount, "blocked",
                new BalanceInfo("user-1", 5000, 5000 - expectedAmount, expectedAmount, "USD", DateTime.UtcNow)));

        var (order, isExisting) = await _sut.Handle(command, CancellationToken.None);

        order.ShouldNotBeNull();
        isExisting.ShouldBeFalse();
        order.Status.ShouldBe(OrderStatus.Reserved.ToString());
        order.Items.Count.ShouldBe(2);
        order.TotalAmount.ShouldBe(expectedAmount);

        _balanceServiceMock.Verify(x => x.CreatePreOrderAsync(It.IsAny<string>(), expectedAmount, It.IsAny<string?>()), Times.Once);
        _orderRepoMock.Verify(x => x.AddAsync(It.Is<Order>(o => o.Status == OrderStatus.Reserved)), Times.Once);
    }

    [Fact]
    public async Task Handle_WithSingleItem_ShouldCalculateTotalCorrectly()
    {
        var command = new CreateOrderCommand
        {
            Items = new List<OrderItemRequest>
            {
                new() { ProductId = "prod-002", Quantity = 3 }
            }
        };

        var expectedAmount = 14.99m * 3; 

        _balanceServiceMock.Setup(x => x.CreatePreOrderAsync(It.IsAny<string>(), expectedAmount, It.IsAny<string?>()))
            .ReturnsAsync(new PreOrderResult(true, "Funds reserved.", "test-id", expectedAmount, "blocked",
                new BalanceInfo("user-1", 5000, 5000 - expectedAmount, expectedAmount, "USD", DateTime.UtcNow)));

        var (order, _) = await _sut.Handle(command, CancellationToken.None);

        order.TotalAmount.ShouldBe(expectedAmount);
        order.Items.Count.ShouldBe(1);
        order.Items[0].ProductName.ShouldBe("Wireless Headphones");
        order.Items[0].Quantity.ShouldBe(3);
    }

    [Fact]
    public async Task Handle_WithInvalidProduct_ShouldThrowProductNotFoundException()
    {
        var command = new CreateOrderCommand
        {
            Items = new List<OrderItemRequest>
            {
                new() { ProductId = "non-existent", Quantity = 1 }
            }
        };

        await Assert.ThrowsAsync<ProductNotFoundException>(() => _sut.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithInsufficientStock_ShouldThrowInsufficientStockException()
    {
        var command = new CreateOrderCommand
        {
            Items = new List<OrderItemRequest>
            {
                new() { ProductId = "prod-003", Quantity = 1 } 
            }
        };

        var exception = await Assert.ThrowsAsync<InsufficientStockException>(
            () => _sut.Handle(command, CancellationToken.None));
        exception.Message.ShouldContain("prod-003");
    }

    [Fact]
    public async Task Handle_WithQuantityExceedingStock_ShouldThrowInsufficientStockException()
    {
        var command = new CreateOrderCommand
        {
            Items = new List<OrderItemRequest>
            {
                new() { ProductId = "prod-004", Quantity = 999 }
            }
        };

        var exception = await Assert.ThrowsAsync<InsufficientStockException>(
            () => _sut.Handle(command, CancellationToken.None));
        exception.Message.ShouldContain("Requested: 999");
        exception.Message.ShouldContain("Available: 15");
    }

    [Fact]
    public async Task Handle_WhenPreOrderFails_ShouldSaveFailedOrderAndThrow()
    {
        var command = new CreateOrderCommand
        {
            Items = new List<OrderItemRequest>
            {
                new() { ProductId = "prod-001", Quantity = 1 }
            }
        };

        _balanceServiceMock.Setup(x => x.CreatePreOrderAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string?>()))
            .ReturnsAsync(new PreOrderResult(false, "Insufficient balance.", It.IsAny<string>(), 19.99m, "failed", default!));

        await Assert.ThrowsAsync<InsufficientBalanceException>(() => _sut.Handle(command, CancellationToken.None));

        _orderRepoMock.Verify(x => x.AddAsync(It.Is<Order>(o => o.Status == OrderStatus.Failed)), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenExternalServiceThrows_ShouldSaveFailedOrderAndThrowExternalServiceException()
    {
        var command = new CreateOrderCommand
        {
            Items = new List<OrderItemRequest>
            {
                new() { ProductId = "prod-001", Quantity = 1 }
            }
        };

        _balanceServiceMock.Setup(x => x.CreatePreOrderAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string?>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var exception = await Assert.ThrowsAsync<ExternalServiceException>(
            () => _sut.Handle(command, CancellationToken.None));
        exception.Message.ShouldContain("BalanceManagement");

        _orderRepoMock.Verify(x => x.AddAsync(It.Is<Order>(o =>
            o.Status == OrderStatus.Failed &&
            o.FailureReason!.Contains("Connection refused"))), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenGetProductsFails_ShouldPropagateException()
    {
        _balanceServiceMock.Setup(x => x.GetProductsAsync())
            .ThrowsAsync(new HttpRequestException("Service unavailable"));

        var command = new CreateOrderCommand
        {
            Items = new List<OrderItemRequest>
            {
                new() { ProductId = "prod-001", Quantity = 1 }
            }
        };

        await Assert.ThrowsAsync<HttpRequestException>(() => _sut.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldAssignUniqueOrderId()
    {
        var command = new CreateOrderCommand
        {
            Items = new List<OrderItemRequest>
            {
                new() { ProductId = "prod-002", Quantity = 1 }
            }
        };

        _balanceServiceMock.Setup(x => x.CreatePreOrderAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string?>()))
            .ReturnsAsync(new PreOrderResult(true, "OK", "id", 14.99m, "blocked",
                new BalanceInfo("user-1", 5000, 4985.01m, 14.99m, "USD", DateTime.UtcNow)));

        var (order, _) = await _sut.Handle(command, CancellationToken.None);

        order.Id.ShouldNotBeNullOrEmpty();
        Guid.TryParse(order.Id, out _).ShouldBeTrue();
    }
}
