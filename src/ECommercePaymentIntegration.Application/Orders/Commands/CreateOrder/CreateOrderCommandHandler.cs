using ECommercePaymentIntegration.Application.Common;
using ECommercePaymentIntegration.Application.DTOs.Responses;
using ECommercePaymentIntegration.Application.Exceptions;
using ECommercePaymentIntegration.Application.ExternalServices;
using ECommercePaymentIntegration.Application.Mappings;
using ECommercePaymentIntegration.Domain.Entities;
using ECommercePaymentIntegration.Domain.Exceptions;
using ECommercePaymentIntegration.Domain.Repositories;
using Microsoft.Extensions.Logging;
using ApplicationException = ECommercePaymentIntegration.Application.Exceptions.ApplicationException;

namespace ECommercePaymentIntegration.Application.Orders.Commands.CreateOrder;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, (OrderResponse Order, bool IsExisting)>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IBalanceManagementService _balanceManagementService;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IBalanceManagementService balanceManagementService,
        ILogger<CreateOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _balanceManagementService = balanceManagementService;
        _logger = logger;
    }

    public async Task<(OrderResponse Order, bool IsExisting)> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // Idempotency check
        if (!string.IsNullOrEmpty(request.IdempotencyKey))
        {
            var existingOrder = await _orderRepository.GetByIdempotencyKeyAsync(request.IdempotencyKey);
            if (existingOrder is not null)
            {
                _logger.LogInformation("Returning existing order {OrderId} for idempotency key {Key}",
                    existingOrder.Id, request.IdempotencyKey);
                return (existingOrder.ToResponse(), true);
            }
        }

        _logger.LogInformation("Creating new order with {ItemCount} items", request.Items.Count);

        // Fetch products to validate and get pricing
        var products = (await _balanceManagementService.GetProductsAsync()).ToList();

        // Build order items with validation
        var orderItems = new List<OrderItem>();
        foreach (var item in request.Items)
        {
            var productDto = products.FirstOrDefault(p => p.Id == item.ProductId)
                ?? throw new ProductNotFoundException(item.ProductId);

            if (productDto.Stock < item.Quantity)
                throw new InsufficientStockException(productDto.Id, item.Quantity, productDto.Stock);

            orderItems.Add(new OrderItem
            {
                ProductId = productDto.Id,
                ProductName = productDto.Name,
                Quantity = item.Quantity,
                UnitPrice = productDto.Price
            });
        }

        // Create order entity
        var order = new Order
        {
            Id = Guid.NewGuid().ToString(),
            Items = orderItems,
            IdempotencyKey = request.IdempotencyKey
        };

        // Reserve funds via Balance Management API (preorder)
        try
        {
            var preOrderResult = await _balanceManagementService.CreatePreOrderAsync(
                order.Id, order.TotalAmount, request.IdempotencyKey);

            if (!preOrderResult.Success)
            {
                order.MarkAsFailed(preOrderResult.Message);
                await _orderRepository.AddAsync(order);

                _logger.LogWarning("Fund reservation failed for order {OrderId}: {Message}",
                    order.Id, preOrderResult.Message);

                throw new InsufficientBalanceException();
            }

            order.MarkAsReserved();
            _logger.LogInformation("Funds reserved for order {OrderId}. Amount: {Amount}",
                order.Id, order.TotalAmount);
        }
        catch (Exception ex) when (ex is not DomainException and not ApplicationException)
        {
            _logger.LogError(ex, "Failed to reserve funds for order {OrderId}", order.Id);

            order.MarkAsFailed($"Payment service error: {ex.Message}");
            await _orderRepository.AddAsync(order);

            throw new ExternalServiceException("BalanceManagement", ex.Message);
        }

        // Persist order
        await _orderRepository.AddAsync(order);

        _logger.LogInformation("Order {OrderId} created successfully with status {Status}",
            order.Id, order.Status);

        return (order.ToResponse(), false);
    }
}
