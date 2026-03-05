using ECommercePaymentIntegration.Application.Common;
using ECommercePaymentIntegration.Application.DTOs.Requests;
using ECommercePaymentIntegration.Application.DTOs.Responses;

namespace ECommercePaymentIntegration.Application.Orders.Commands.CreateOrder;

public class CreateOrderCommand : IRequest<(OrderResponse Order, bool IsExisting)>
{
    public List<OrderItemRequest> Items { get; set; } = new();
    public string? IdempotencyKey { get; set; }
}
