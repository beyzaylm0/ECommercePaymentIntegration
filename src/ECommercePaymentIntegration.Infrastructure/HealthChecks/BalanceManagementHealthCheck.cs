using ECommercePaymentIntegration.Infrastructure.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace ECommercePaymentIntegration.Infrastructure.HealthChecks;

public class BalanceManagementHealthCheck : IHealthCheck
{
    private readonly HttpClient _httpClient;
    private readonly BalanceManagementSettings _settings;

    public BalanceManagementHealthCheck(
        IHttpClientFactory httpClientFactory,
        IOptions<BalanceManagementSettings> settings)
    {
        _httpClient = httpClientFactory.CreateClient("HealthCheck");
        _settings = settings.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"{_settings.BaseUrl}/api/products",
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("Balance Management service is reachable.");
            }

            return HealthCheckResult.Degraded(
                $"Balance Management service returned {response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Balance Management service is unreachable.",
                ex);
        }
    }
}
