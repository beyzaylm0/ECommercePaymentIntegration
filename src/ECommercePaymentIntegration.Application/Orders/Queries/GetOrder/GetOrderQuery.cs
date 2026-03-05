using ECommercePaymentIntegration.Application.Common;
using ECommercePaymentIntegration.Application.DTOs.Responses;

namespace ECommercePaymentIntegration.Application.Orders.Queries.GetOrder;

public class GetOrderQuery : IRequest<OrderResponse?>
{
    public string OrderId { get; set; } = string.Empty;
}
