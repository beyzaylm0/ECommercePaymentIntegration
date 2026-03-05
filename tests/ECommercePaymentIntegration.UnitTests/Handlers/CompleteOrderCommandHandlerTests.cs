using ECommercePaymentIntegration.Application.DTOs.ExternalServices;
using ECommercePaymentIntegration.Application.Exceptions;
using ECommercePaymentIntegration.Application.ExternalServices;
using ECommercePaymentIntegration.Application.Orders.Commands.CompleteOrder;
using ECommercePaymentIntegration.Domain.Entities;
using ECommercePaymentIntegration.Domain.Enums;
using ECommercePaymentIntegration.Domain.Exceptions;
using ECommercePaymentIntegration.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace ECommercePaymentIntegration.UnitTests.Handlers;

public class CompleteOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepoMock;
    private readonly Mock<IBalanceManagementService> _balanceServiceMock;
    private readonly Mock<ILogger<CompleteOrderCommandHandler>> _loggerMock;
    private readonly CompleteOrderCommandHandler _sut;

    public CompleteOrderCommandHandlerTests()
    {
        _orderRepoMock = new Mock<IOrderRepository>();
        _balanceServiceMock = new Mock<IBalanceManagementService>();
        _loggerMock = new Mock<ILogger<CompleteOrderCommandHandler>>();

        _sut = new CompleteOrderCommandHandler(
            _orderRepoMock.Object,
            _balanceServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithReservedOrder_ShouldCompleteSuccessfully()
    {
        var orderId = "order-123";
        var order = new Order
        {
            Id = orderId,
            Status = OrderStatus.Reserved,
            Items = new List<OrderItem>
            {
                new() { ProductId = "prod-001", ProductName = "Premium Smartphone", Quantity = 1, UnitPrice = 19.99m }
            }
        };

        _orderRepoMock.Setup(x => x.GetByIdAsync(orderId)).ReturnsAsync(order);
        _balanceServiceMock.Setup(x => x.CompleteOrderAsync(orderId))
            .ReturnsAsync(new CompleteOrderResult(true, "Order completed.", orderId, "completed",
                new BalanceInfo("user-1", 4980.01m, 4980.01m, 0, "USD", DateTime.UtcNow)));

        var result = await _sut.Handle(new CompleteOrderCommand { OrderId = orderId }, CancellationToken.None);

        result.ShouldNotBeNull();
        result.Status.ShouldBe(OrderStatus.Completed.ToString());
        result.CompletedAt.ShouldNotBeNull();

        _orderRepoMock.Verify(x => x.UpdateAsync(It.Is<Order>(o => o.Status == OrderStatus.Completed)), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentOrder_ShouldThrowOrderNotFoundException()
    {
        _orderRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((Order?)null);

        await Assert.ThrowsAsync<OrderNotFoundException>(
            () => _sut.Handle(new CompleteOrderCommand { OrderId = "non-existent" }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithCompletedOrder_ShouldReturnIdempotentResponse()
    {
        var order = new Order { Id = "order-123", Status = OrderStatus.Completed };
        _orderRepoMock.Setup(x => x.GetByIdAsync("order-123")).ReturnsAsync(order);

        var result = await _sut.Handle(new CompleteOrderCommand { OrderId = "order-123" }, CancellationToken.None);

        result.ShouldNotBeNull();
        result.Status.ShouldBe(OrderStatus.Completed.ToString());
    }

    [Fact]
    public async Task Handle_WithPendingOrder_ShouldThrowInvalidOrderStateException()
    {
        var order = new Order { Id = "order-456", Status = OrderStatus.Pending };
        _orderRepoMock.Setup(x => x.GetByIdAsync("order-456")).ReturnsAsync(order);

        await Assert.ThrowsAsync<InvalidOrderStateException>(
            () => _sut.Handle(new CompleteOrderCommand { OrderId = "order-456" }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithCancelledOrder_ShouldThrowInvalidOrderStateException()
    {
        var order = new Order { Id = "order-789", Status = OrderStatus.Cancelled };
        _orderRepoMock.Setup(x => x.GetByIdAsync("order-789")).ReturnsAsync(order);

        await Assert.ThrowsAsync<InvalidOrderStateException>(
            () => _sut.Handle(new CompleteOrderCommand { OrderId = "order-789" }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenExternalServiceFails_ShouldThrowExternalServiceException()
    {
        var order = new Order { Id = "order-123", Status = OrderStatus.Reserved };
        _orderRepoMock.Setup(x => x.GetByIdAsync("order-123")).ReturnsAsync(order);
        _balanceServiceMock.Setup(x => x.CompleteOrderAsync("order-123"))
            .ThrowsAsync(new HttpRequestException("Timeout"));

        var exception = await Assert.ThrowsAsync<ExternalServiceException>(
            () => _sut.Handle(new CompleteOrderCommand { OrderId = "order-123" }, CancellationToken.None));
        exception.Message.ShouldContain("BalanceManagement");
    }

    [Fact]
    public async Task Handle_WhenServiceReturnsFailure_ShouldThrowExternalServiceException()
    {
        var order = new Order { Id = "order-123", Status = OrderStatus.Reserved };
        _orderRepoMock.Setup(x => x.GetByIdAsync("order-123")).ReturnsAsync(order);
        _balanceServiceMock.Setup(x => x.CompleteOrderAsync("order-123"))
            .ReturnsAsync(new CompleteOrderResult(false, "Pre-order not found.", "order-123", "failed", default!));

        var exception = await Assert.ThrowsAsync<ExternalServiceException>(
            () => _sut.Handle(new CompleteOrderCommand { OrderId = "order-123" }, CancellationToken.None));
        exception.Message.ShouldContain("Pre-order not found");
    }
}
