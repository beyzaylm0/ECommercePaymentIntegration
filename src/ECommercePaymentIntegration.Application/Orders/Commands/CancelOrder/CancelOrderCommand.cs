using ECommercePaymentIntegration.Application.Common;
using ECommercePaymentIntegration.Application.DTOs.Responses;

namespace ECommercePaymentIntegration.Application.Orders.Commands.CancelOrder;

public class CancelOrderCommand : IRequest<OrderResponse>
{
    public string OrderId { get; set; } = string.Empty;
}
