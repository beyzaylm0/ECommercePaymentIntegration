using ECommercePaymentIntegration.Application.Common;
using ECommercePaymentIntegration.Application.DTOs.Responses;

namespace ECommercePaymentIntegration.Application.Orders.Commands.CompleteOrder;

public class CompleteOrderCommand : IRequest<OrderResponse>
{
    public string OrderId { get; set; } = string.Empty;
}
