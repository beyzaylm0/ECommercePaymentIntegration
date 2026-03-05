using ECommercePaymentIntegration.Application.DTOs.ExternalServices;
using ECommercePaymentIntegration.Application.ExternalServices;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ECommercePaymentIntegration.Infrastructure.Caching;

public class CachedBalanceManagementService : IBalanceManagementService
{
    private readonly IBalanceManagementService _inner;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedBalanceManagementService> _logger;

    private const string ProductsCacheKey = "products_list";
    private static readonly TimeSpan ProductsCacheDuration = TimeSpan.FromMinutes(5);

    public CachedBalanceManagementService(
        IBalanceManagementService inner,
        IMemoryCache cache,
        ILogger<CachedBalanceManagementService> logger)
    {
        _inner = inner;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IEnumerable<ProductDto>> GetProductsAsync()
    {
        if (_cache.TryGetValue(ProductsCacheKey, out IEnumerable<ProductDto>? cachedProducts) && cachedProducts != null)
        {
            _logger.LogDebug("Returning cached products");
            return cachedProducts;
        }

        _logger.LogInformation("Cache miss for products, fetching from service");
        var products = await _inner.GetProductsAsync();

        var productList = products.ToList();
        _cache.Set(ProductsCacheKey, (IEnumerable<ProductDto>)productList, ProductsCacheDuration);

        return productList;
    }

    public Task<BalanceInfo> GetBalanceAsync()
        => _inner.GetBalanceAsync();

    public Task<PreOrderResult> CreatePreOrderAsync(string orderId, decimal amount, string? idempotencyKey = null)
    {
        //stock may change
        _cache.Remove(ProductsCacheKey);
        return _inner.CreatePreOrderAsync(orderId, amount, idempotencyKey);
    }

    public Task<CompleteOrderResult> CompleteOrderAsync(string orderId)
        => _inner.CompleteOrderAsync(orderId);

    public Task<CancelOrderResult> CancelOrderAsync(string orderId)
    {
        _cache.Remove(ProductsCacheKey);
        return _inner.CancelOrderAsync(orderId);
    }
}
