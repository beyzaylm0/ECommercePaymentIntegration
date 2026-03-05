using ECommercePaymentIntegration.Application.Common;
using ECommercePaymentIntegration.Application.DTOs.Responses;
using ECommercePaymentIntegration.Application.Exceptions;
using ECommercePaymentIntegration.Application.ExternalServices;
using ECommercePaymentIntegration.Application.Mappings;
using ECommercePaymentIntegration.Domain.Enums;
using ECommercePaymentIntegration.Domain.Exceptions;
using ECommercePaymentIntegration.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace ECommercePaymentIntegration.Application.Orders.Commands.CancelOrder;

public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, OrderResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IBalanceManagementService _balanceManagementService;
    private readonly ILogger<CancelOrderCommandHandler> _logger;

    public CancelOrderCommandHandler(
        IOrderRepository orderRepository,
        IBalanceManagementService balanceManagementService,
        ILogger<CancelOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _balanceManagementService = balanceManagementService;
        _logger = logger;
    }

    public async Task<OrderResponse> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Transitioning order {OrderId} to {Status}", request.OrderId, OrderStatus.Cancelled);

        var order = await _orderRepository.GetByIdAsync(request.OrderId)
            ?? throw new OrderNotFoundException(request.OrderId);

        if (order.Status == OrderStatus.Cancelled)
        {
            _logger.LogInformation("Order {OrderId} is already {Status}, returning idempotent response",
                request.OrderId, OrderStatus.Cancelled);
            return order.ToResponse();
        }

        if (order.Status != OrderStatus.Reserved)
            throw new InvalidOrderStateException(request.OrderId, order.Status, OrderStatus.Reserved);

        try
        {
            var result = await _balanceManagementService.CancelOrderAsync(request.OrderId);

            if (!result.Success)
            {
                _logger.LogWarning("Failed to transition order {OrderId} to {Status}: {Message}",
                    request.OrderId, OrderStatus.Cancelled, result.Message);
                throw new ExternalServiceException("BalanceManagement", result.Message);
            }

            order.MarkAsCancelled();
            await _orderRepository.UpdateAsync(order);

            _logger.LogInformation("Order {OrderId} transitioned to {Status} successfully",
                request.OrderId, OrderStatus.Cancelled);
        }
        catch (Exception ex) when (ex is not DomainException)
        {
            _logger.LogError(ex, "Failed to transition order {OrderId} to {Status}",
                request.OrderId, OrderStatus.Cancelled);
            throw new ExternalServiceException("BalanceManagement", ex.Message);
        }

        return order.ToResponse();
    }
}
