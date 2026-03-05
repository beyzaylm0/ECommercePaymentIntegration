using ECommercePaymentIntegration.Application.DTOs.ExternalServices;
using ECommercePaymentIntegration.Application.Exceptions;
using ECommercePaymentIntegration.Application.ExternalServices;
using ECommercePaymentIntegration.Application.Orders.Commands.CancelOrder;
using ECommercePaymentIntegration.Domain.Entities;
using ECommercePaymentIntegration.Domain.Enums;
using ECommercePaymentIntegration.Domain.Exceptions;
using ECommercePaymentIntegration.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace ECommercePaymentIntegration.UnitTests.Handlers;

public class CancelOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepoMock;
    private readonly Mock<IBalanceManagementService> _balanceServiceMock;
    private readonly Mock<ILogger<CancelOrderCommandHandler>> _loggerMock;
    private readonly CancelOrderCommandHandler _sut;

    public CancelOrderCommandHandlerTests()
    {
        _orderRepoMock = new Mock<IOrderRepository>();
        _balanceServiceMock = new Mock<IBalanceManagementService>();
        _loggerMock = new Mock<ILogger<CancelOrderCommandHandler>>();

        _sut = new CancelOrderCommandHandler(
            _orderRepoMock.Object,
            _balanceServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithReservedOrder_ShouldCancelSuccessfully()
    {
        var orderId = "order-456";
        var order = new Order
        {
            Id = orderId,
            Status = OrderStatus.Reserved,
            Items = new List<OrderItem>
            {
                new() { ProductId = "prod-002", ProductName = "Wireless Headphones", Quantity = 1, UnitPrice = 14.99m }
            }
        };

        _orderRepoMock.Setup(x => x.GetByIdAsync(orderId)).ReturnsAsync(order);
        _balanceServiceMock.Setup(x => x.CancelOrderAsync(orderId))
            .ReturnsAsync(new CancelOrderResult(true, "Order cancelled.", orderId, "cancelled",
                new BalanceInfo("user-1", 5000, 5000, 0, "USD", DateTime.UtcNow)));

        var result = await _sut.Handle(new CancelOrderCommand { OrderId = orderId }, CancellationToken.None);

        result.Status.ShouldBe(OrderStatus.Cancelled.ToString());
        result.CancelledAt.ShouldNotBeNull();
        _orderRepoMock.Verify(x => x.UpdateAsync(It.Is<Order>(o => o.Status == OrderStatus.Cancelled)), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentOrder_ShouldThrowOrderNotFoundException()
    {
        _orderRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((Order?)null);

        await Assert.ThrowsAsync<OrderNotFoundException>(
            () => _sut.Handle(new CancelOrderCommand { OrderId = "non-existent" }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithCompletedOrder_ShouldThrowInvalidOrderStateException()
    {
        var order = new Order { Id = "order-completed", Status = OrderStatus.Completed };
        _orderRepoMock.Setup(x => x.GetByIdAsync("order-completed")).ReturnsAsync(order);

        await Assert.ThrowsAsync<InvalidOrderStateException>(
            () => _sut.Handle(new CancelOrderCommand { OrderId = "order-completed" }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenExternalServiceFails_ShouldThrowExternalServiceException()
    {
        var order = new Order { Id = "order-456", Status = OrderStatus.Reserved };
        _orderRepoMock.Setup(x => x.GetByIdAsync("order-456")).ReturnsAsync(order);
        _balanceServiceMock.Setup(x => x.CancelOrderAsync("order-456"))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var exception = await Assert.ThrowsAsync<ExternalServiceException>(
            () => _sut.Handle(new CancelOrderCommand { OrderId = "order-456" }, CancellationToken.None));
        exception.Message.ShouldContain("BalanceManagement");
    }

    [Fact]
    public async Task Handle_WhenServiceReturnsFailure_ShouldThrowExternalServiceException()
    {
        var order = new Order { Id = "order-456", Status = OrderStatus.Reserved };
        _orderRepoMock.Setup(x => x.GetByIdAsync("order-456")).ReturnsAsync(order);
        _balanceServiceMock.Setup(x => x.CancelOrderAsync("order-456"))
            .ReturnsAsync(new CancelOrderResult(false, "Pre-order not found.", "order-456", "failed", default!));

        await Assert.ThrowsAsync<ExternalServiceException>(
            () => _sut.Handle(new CancelOrderCommand { OrderId = "order-456" }, CancellationToken.None));
    }
}
