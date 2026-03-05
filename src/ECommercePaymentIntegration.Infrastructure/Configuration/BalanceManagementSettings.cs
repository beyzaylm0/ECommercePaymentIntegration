namespace ECommercePaymentIntegration.Infrastructure.Configuration;

public class BalanceManagementSettings
{
    public const string SectionName = "BalanceManagement";
    public string BaseUrl { get; set; } = "https://balance-management-pi44.onrender.com";
    public int TimeoutSeconds { get; set; } = 30;
    public int RetryCount { get; set; } = 3;
    public int CircuitBreakerThreshold { get; set; } = 5;
    public int CircuitBreakerDurationSeconds { get; set; } = 30;
}
