using ECommercePaymentIntegration.Application.Common;
using ECommercePaymentIntegration.Application.DTOs.Responses;
using ECommercePaymentIntegration.Application.Products.Queries.GetAllProducts;
using Microsoft.AspNetCore.Mvc;

namespace ECommercePaymentIntegration.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all available products with pricing information.
    /// </summary>
    /// <returns>List of products from Balance Management service.</returns>
    /// <response code="200">Returns the list of products</response>
    /// <response code="502">Balance Management service is unavailable</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProductResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> GetProducts()
    {
        var products = await _mediator.Send<IEnumerable<ProductResponse>>(new GetAllProductsQuery());
        return Ok(ApiResponse<IEnumerable<ProductResponse>>.SuccessResponse(products, "Products retrieved successfully."));
    }
}
