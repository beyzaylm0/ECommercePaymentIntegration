using ECommercePaymentIntegration.Application.DTOs.ExternalServices;
using ECommercePaymentIntegration.Application.ExternalServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;

namespace ECommercePaymentIntegration.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public Mock<IBalanceManagementService> BalanceServiceMock { get; } = new();

    public CustomWebApplicationFactory()
    {
        SetupDefaultMocks();
    }

    private void SetupDefaultMocks()
    {
        // Setup default product responses (matches real Balance Management API data)
        BalanceServiceMock.Setup(x => x.GetProductsAsync())
            .ReturnsAsync(new List<ProductDto>
            {
                new("prod-001", "Premium Smartphone", "Latest model with advanced features", 19.99m, "USD", "Electronics", 42),
                new("prod-002", "Wireless Headphones", "Noise-cancelling with premium sound quality", 14.99m, "USD", "Electronics", 78),
                new("prod-003", "Smart Watch", "Fitness tracking and notifications", 12.99m, "USD", "Electronics", 0),
                new("prod-004", "Laptop", "High-performance for work and gaming", 19.99m, "USD", "Electronics", 15),
                new("prod-005", "Wireless Charger", "Fast charging for compatible devices", 9.99m, "USD", "Accessories", 120)
            });

        BalanceServiceMock.Setup(x => x.CreatePreOrderAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string?>()))
            .ReturnsAsync((string orderId, decimal amount, string? idempotencyKey) =>
                new PreOrderResult(true, "Funds reserved.", orderId, amount, "blocked",
                    new BalanceInfo("user-1", 5000, 5000 - amount, amount, "USD", DateTime.UtcNow)));

        BalanceServiceMock.Setup(x => x.CompleteOrderAsync(It.IsAny<string>()))
            .ReturnsAsync((string orderId) =>
                new CompleteOrderResult(true, "Order completed.", orderId, "completed",
                    new BalanceInfo("user-1", 4000, 4000, 0, "USD", DateTime.UtcNow)));

        BalanceServiceMock.Setup(x => x.CancelOrderAsync(It.IsAny<string>()))
            .ReturnsAsync((string orderId) =>
                new CancelOrderResult(true, "Order cancelled.", orderId, "cancelled",
                    new BalanceInfo("user-1", 5000, 5000, 0, "USD", DateTime.UtcNow)));
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptorsToRemove = services
                .Where(d =>
                    d.ServiceType == typeof(IBalanceManagementService) ||
                    d.ImplementationType?.Name.Contains("BalanceManagement") == true ||
                    d.ImplementationType?.Name.Contains("CachedBalance") == true)
                .ToList();

            foreach (var d in descriptorsToRemove)
                services.Remove(d);

            var httpClientDescriptors = services
                .Where(d => d.ServiceType.Name.Contains("HttpMessageHandler") &&
                            d.ImplementationType?.FullName?.Contains("BalanceManagement") == true)
                .ToList();
            foreach (var d in httpClientDescriptors)
                services.Remove(d);

            services.AddScoped<IBalanceManagementService>(_ => BalanceServiceMock.Object);

            // Remove the external service health check so /health returns "Healthy" in tests
            services.Configure<HealthCheckServiceOptions>(options =>
            {
                var registration = options.Registrations
                    .FirstOrDefault(r => r.Name == "balance-management-api");
                if (registration != null)
                    options.Registrations.Remove(registration);
            });
        });
    }
}
