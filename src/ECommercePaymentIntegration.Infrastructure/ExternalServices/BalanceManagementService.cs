using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ECommercePaymentIntegration.Application.Exceptions;
using ECommercePaymentIntegration.Application.DTOs.ExternalServices;
using ECommercePaymentIntegration.Application.ExternalServices;
using Microsoft.Extensions.Logging;

namespace ECommercePaymentIntegration.Infrastructure.ExternalServices;

public class BalanceManagementService : IBalanceManagementService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BalanceManagementService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public BalanceManagementService(HttpClient httpClient, ILogger<BalanceManagementService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IEnumerable<ProductDto>> GetProductsAsync()
    {
        _logger.LogInformation("Fetching products from Balance Management API.");

        var response = await _httpClient.GetAsync("/api/products");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ApiResult<List<ProductDto>>>(JsonOptions);

        if (result is null || !result.Success)
            throw new ExternalServiceException("BalanceManagement", "Failed to fetch products.");

        return result.Data ?? Enumerable.Empty<ProductDto>();
    }

    public async Task<BalanceInfo> GetBalanceAsync()
    {
        _logger.LogInformation("Fetching balance from Balance Management API.");

        var response = await _httpClient.GetAsync("/api/balance");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ApiResult<BalanceData>>(JsonOptions);

        if (result?.Data is null)
            throw new ExternalServiceException("BalanceManagement", "Failed to fetch balance.");

        return new BalanceInfo(
            result.Data.UserId,
            result.Data.TotalBalance,
            result.Data.AvailableBalance,
            result.Data.BlockedBalance,
            result.Data.Currency,
            result.Data.LastUpdated);
    }

    public async Task<PreOrderResult> CreatePreOrderAsync(string orderId, decimal amount, string? idempotencyKey = null)
    {
        _logger.LogInformation("Creating pre-order {OrderId} with amount {Amount}.", orderId, amount);

        var payload = new { orderId, amount, idempotencyKey };
        var response = await _httpClient.PostAsJsonAsync("/api/balance/preorder", payload, JsonOptions);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Pre-order failed for {OrderId}. Status: {Status}, Response: {Response}",
                orderId, response.StatusCode, errorContent);

            try
            {
                var errorResult = JsonSerializer.Deserialize<ErrorResponse>(errorContent, JsonOptions);
                return new PreOrderResult(false, errorResult?.Message ?? "Pre-order failed.", orderId, amount, "failed", default!);
            }
            catch
            {
                return new PreOrderResult(false, $"Pre-order failed with status {response.StatusCode}.", orderId, amount, "failed", default!);
            }
        }

        var result = await response.Content.ReadFromJsonAsync<PreOrderApiResult>(JsonOptions);

        if (result is null || !result.Success)
            return new PreOrderResult(false, result?.Message ?? "Pre-order failed.", orderId, amount, "failed", default!);

        var balance = result.Data?.UpdatedBalance;
        var balanceInfo = balance is not null
            ? new BalanceInfo(balance.UserId, balance.TotalBalance, balance.AvailableBalance, balance.BlockedBalance, balance.Currency, balance.LastUpdated)
            : default!;

        return new PreOrderResult(true, result.Message ?? "Pre-order created.", orderId, amount, "blocked", balanceInfo);
    }

    public async Task<CompleteOrderResult> CompleteOrderAsync(string orderId)
    {
        _logger.LogInformation("Completing order {OrderId} via Balance Management API.", orderId);

        var payload = new { orderId };
        var response = await _httpClient.PostAsJsonAsync("/api/balance/complete", payload, JsonOptions);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Complete order failed for {OrderId}. Status: {Status}, Response: {Response}",
                orderId, response.StatusCode, errorContent);

            try
            {
                var errorResult = JsonSerializer.Deserialize<ErrorResponse>(errorContent, JsonOptions);
                throw new ExternalServiceException("BalanceManagement", errorResult?.Message ?? "Complete order failed.");
            }
            catch (ExternalServiceException)
            {
                throw;
            }
            catch
            {
                throw new ExternalServiceException("BalanceManagement", $"Complete order failed with status {response.StatusCode}.");
            }
        }

        var result = await response.Content.ReadFromJsonAsync<CompleteOrderApiResult>(JsonOptions);

        if (result is null || !result.Success)
            throw new ExternalServiceException("BalanceManagement", result?.Message ?? "Complete order failed.");

        var balance = result.Data?.UpdatedBalance;
        var balanceInfo = balance is not null
            ? new BalanceInfo(balance.UserId, balance.TotalBalance, balance.AvailableBalance, balance.BlockedBalance, balance.Currency, balance.LastUpdated)
            : default!;

        return new CompleteOrderResult(true, result.Message ?? "Order completed.", orderId, "completed", balanceInfo);
    }

    public async Task<CancelOrderResult> CancelOrderAsync(string orderId)
    {
        _logger.LogInformation("Cancelling order {OrderId} via Balance Management API.", orderId);

        var payload = new { orderId };
        var response = await _httpClient.PostAsJsonAsync("/api/balance/cancel", payload, JsonOptions);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Cancel order failed for {OrderId}. Status: {Status}, Response: {Response}",
                orderId, response.StatusCode, errorContent);

            try
            {
                var errorResult = JsonSerializer.Deserialize<ErrorResponse>(errorContent, JsonOptions);
                throw new ExternalServiceException("BalanceManagement", errorResult?.Message ?? "Cancel order failed.");
            }
            catch (ExternalServiceException)
            {
                throw;
            }
            catch
            {
                throw new ExternalServiceException("BalanceManagement", $"Cancel order failed with status {response.StatusCode}.");
            }
        }

        var result = await response.Content.ReadFromJsonAsync<CancelOrderApiResult>(JsonOptions);

        if (result is null || !result.Success)
            throw new ExternalServiceException("BalanceManagement", result?.Message ?? "Cancel order failed.");

        var balance = result.Data?.UpdatedBalance;
        var balanceInfo = balance is not null
            ? new BalanceInfo(balance.UserId, balance.TotalBalance, balance.AvailableBalance, balance.BlockedBalance, balance.Currency, balance.LastUpdated)
            : default!;

        return new CancelOrderResult(true, result.Message ?? "Order cancelled.", orderId, "cancelled", balanceInfo);
    }

    #region API Response Models

    private class ApiResult<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
    }

    private class ErrorResponse
    {
        public string? Error { get; set; }
        public string? Message { get; set; }
    }

    private class BalanceData
    {
        public string UserId { get; set; } = string.Empty;
        public decimal TotalBalance { get; set; }
        public decimal AvailableBalance { get; set; }
        public decimal BlockedBalance { get; set; }
        public string Currency { get; set; } = "USD";
        public DateTime LastUpdated { get; set; }
    }

    private class PreOrderApiResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public PreOrderData? Data { get; set; }
    }

    private class PreOrderData
    {
        public PreOrderInfo? PreOrder { get; set; }
        public BalanceData? UpdatedBalance { get; set; }
    }

    private class PreOrderInfo
    {
        public string OrderId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    private class CompleteOrderApiResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public CompleteOrderData? Data { get; set; }
    }

    private class CompleteOrderData
    {
        public OrderInfo? Order { get; set; }
        public BalanceData? UpdatedBalance { get; set; }
    }

    private class CancelOrderApiResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public CancelOrderData? Data { get; set; }
    }

    private class CancelOrderData
    {
        public OrderInfo? Order { get; set; }
        public BalanceData? UpdatedBalance { get; set; }
    }

    private class OrderInfo
    {
        public string OrderId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    #endregion
}
