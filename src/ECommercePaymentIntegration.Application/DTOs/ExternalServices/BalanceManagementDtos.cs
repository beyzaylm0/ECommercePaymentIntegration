namespace ECommercePaymentIntegration.Application.DTOs.ExternalServices;

public record ProductDto(
    string Id,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    string Category,
    int Stock);

public record BalanceInfo(
    string UserId,
    decimal TotalBalance,
    decimal AvailableBalance,
    decimal BlockedBalance,
    string Currency,
    DateTime LastUpdated);

public record PreOrderResult(
    bool Success,
    string Message,
    string OrderId,
    decimal Amount,
    string Status,
    BalanceInfo UpdatedBalance);

public record CompleteOrderResult(
    bool Success,
    string Message,
    string OrderId,
    string Status,
    BalanceInfo UpdatedBalance);

public record CancelOrderResult(
    bool Success,
    string Message,
    string OrderId,
    string Status,
    BalanceInfo UpdatedBalance);
