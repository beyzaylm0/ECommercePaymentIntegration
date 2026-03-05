using ECommercePaymentIntegration.Application.Common;
using ECommercePaymentIntegration.Application.DTOs.Responses;

namespace ECommercePaymentIntegration.Application.Orders.Queries.GetAllOrders;

public class GetAllOrdersQuery : IRequest<IEnumerable<OrderResponse>>
{
}
