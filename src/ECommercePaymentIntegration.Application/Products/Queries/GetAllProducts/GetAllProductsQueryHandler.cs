using ECommercePaymentIntegration.Application.Common;
using ECommercePaymentIntegration.Application.DTOs.Responses;
using ECommercePaymentIntegration.Application.ExternalServices;
using ECommercePaymentIntegration.Application.Mappings;
using Microsoft.Extensions.Logging;

namespace ECommercePaymentIntegration.Application.Products.Queries.GetAllProducts;

public class GetAllProductsQueryHandler : IRequestHandler<GetAllProductsQuery, IEnumerable<ProductResponse>>
{
    private readonly IBalanceManagementService _balanceManagementService;
    private readonly ILogger<GetAllProductsQueryHandler> _logger;

    public GetAllProductsQueryHandler(
        IBalanceManagementService balanceManagementService,
        ILogger<GetAllProductsQueryHandler> logger)
    {
        _balanceManagementService = balanceManagementService;
        _logger = logger;
    }

    public async Task<IEnumerable<ProductResponse>> Handle(GetAllProductsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching products from Balance Management service");

        var products = (await _balanceManagementService.GetProductsAsync()).ToList();

        _logger.LogInformation("Retrieved {ProductCount} products", products.Count);

        return products.Select(p => p.ToResponse());
    }
}
