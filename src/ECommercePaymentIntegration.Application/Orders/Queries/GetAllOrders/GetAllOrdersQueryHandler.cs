using ECommercePaymentIntegration.Application.Common;
using ECommercePaymentIntegration.Application.DTOs.Responses;
using ECommercePaymentIntegration.Application.Mappings;
using ECommercePaymentIntegration.Domain.Repositories;

namespace ECommercePaymentIntegration.Application.Orders.Queries.GetAllOrders;

public class GetAllOrdersQueryHandler : IRequestHandler<GetAllOrdersQuery, IEnumerable<OrderResponse>>
{
    private readonly IOrderRepository _orderRepository;

    public GetAllOrdersQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<IEnumerable<OrderResponse>> Handle(GetAllOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = await _orderRepository.GetAllAsync();
        return orders.Select(o => o.ToResponse());
    }
}
