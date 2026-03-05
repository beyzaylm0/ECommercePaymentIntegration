using ECommercePaymentIntegration.Application.DTOs.ExternalServices;

namespace ECommercePaymentIntegration.Application.ExternalServices;

public interface IBalanceManagementService
{
    Task<IEnumerable<ProductDto>> GetProductsAsync();
    Task<BalanceInfo> GetBalanceAsync();
    Task<PreOrderResult> CreatePreOrderAsync(string orderId, decimal amount, string? idempotencyKey = null);
    Task<CompleteOrderResult> CompleteOrderAsync(string orderId);
    Task<CancelOrderResult> CancelOrderAsync(string orderId);
}
