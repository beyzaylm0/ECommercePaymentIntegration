using ECommercePaymentIntegration.Application.ExternalServices;
using ECommercePaymentIntegration.Domain.Repositories;
using ECommercePaymentIntegration.Infrastructure.Caching;
using ECommercePaymentIntegration.Infrastructure.Configuration;
using ECommercePaymentIntegration.Infrastructure.ExternalServices;
using ECommercePaymentIntegration.Infrastructure.HealthChecks;
using ECommercePaymentIntegration.Infrastructure.Http;
using ECommercePaymentIntegration.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace ECommercePaymentIntegration.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var balanceSettings = configuration
            .GetSection(BalanceManagementSettings.SectionName)
            .Get<BalanceManagementSettings>() ?? new BalanceManagementSettings();

        services.Configure<BalanceManagementSettings>(
            configuration.GetSection(BalanceManagementSettings.SectionName));

        services.AddHttpContextAccessor();

        services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();

        services.AddMemoryCache();

        services.AddTransient<CorrelationIdDelegatingHandler>();

        services.AddHttpClient<BalanceManagementService>(client =>
        {
            client.BaseAddress = new Uri(balanceSettings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(balanceSettings.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
        .AddPolicyHandler((serviceProvider, _) => GetRetryPolicy(balanceSettings, serviceProvider))
        .AddPolicyHandler((serviceProvider, _) => GetCircuitBreakerPolicy(balanceSettings, serviceProvider));

        services.AddScoped<IBalanceManagementService>(sp =>
        {
            var inner = sp.GetRequiredService<BalanceManagementService>();
            var cache = sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
            var logger = sp.GetRequiredService<ILogger<CachedBalanceManagementService>>();
            return new CachedBalanceManagementService(inner, cache, logger);
        });

        services.AddHttpClient("HealthCheck", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        services.AddHealthChecks()
            .AddCheck<BalanceManagementHealthCheck>(
                "balance-management-api",
                tags: new[] { "external", "api" });

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(
        BalanceManagementSettings settings,
        IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<BalanceManagementService>>();

        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                retryCount: settings.RetryCount,
                sleepDurationProvider: retryAttempt =>
                {
                    // Exponential backoff with jitter
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                    var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 500));
                    return delay + jitter;
                },
                onRetry: (outcome, timespan, retryAttempt, _) =>
                {
                    logger.LogWarning(
                        "Retry attempt {RetryAttempt} after {Delay}s. Reason: {Reason}",
                        retryAttempt,
                        timespan.TotalSeconds,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                });
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(
        BalanceManagementSettings settings,
        IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<BalanceManagementService>>();

        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: settings.CircuitBreakerThreshold,
                durationOfBreak: TimeSpan.FromSeconds(settings.CircuitBreakerDurationSeconds),
                onBreak: (outcome, breakDelay) =>
                {
                    logger.LogWarning(
                        "Circuit breaker OPEN for {BreakDuration}s. Reason: {Reason}",
                        breakDelay.TotalSeconds,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                },
                onReset: () => logger.LogInformation("Circuit breaker CLOSED - normal operations resumed."),
                onHalfOpen: () => logger.LogInformation("Circuit breaker HALF-OPEN - testing..."));
    }
}
