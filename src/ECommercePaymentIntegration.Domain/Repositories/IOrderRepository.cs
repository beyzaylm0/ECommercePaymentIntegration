using ECommercePaymentIntegration.Domain.Entities;

namespace ECommercePaymentIntegration.Domain.Repositories;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(string id);
    Task<Order?> GetByIdempotencyKeyAsync(string idempotencyKey);
    Task<IEnumerable<Order>> GetAllAsync();
    Task AddAsync(Order order);
    Task UpdateAsync(Order order);
}
