using System.Collections.Concurrent;
using ECommercePaymentIntegration.Domain.Entities;
using ECommercePaymentIntegration.Domain.Repositories;

namespace ECommercePaymentIntegration.Infrastructure.Repositories;

public class InMemoryOrderRepository : IOrderRepository
{
    private readonly ConcurrentDictionary<string, Order> _orders = new();
    private readonly ConcurrentDictionary<string, Order> _idempotencyIndex = new();

    public Task<Order?> GetByIdAsync(string orderId)
    {
        _orders.TryGetValue(orderId, out var order);
        return Task.FromResult(order);
    }

    public Task<Order?> GetByIdempotencyKeyAsync(string idempotencyKey)
    {
        _idempotencyIndex.TryGetValue(idempotencyKey, out var order);
        return Task.FromResult(order);
    }

    public Task<IEnumerable<Order>> GetAllAsync()
    {
        var orders = _orders.Values.OrderByDescending(o => o.CreatedAt).AsEnumerable();
        return Task.FromResult(orders);
    }

    public Task AddAsync(Order order)
    {
        _orders.TryAdd(order.Id, order);
        if (!string.IsNullOrEmpty(order.IdempotencyKey))
            _idempotencyIndex.TryAdd(order.IdempotencyKey, order);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Order order)
    {
        _orders[order.Id] = order;
        return Task.CompletedTask;
    }
}
