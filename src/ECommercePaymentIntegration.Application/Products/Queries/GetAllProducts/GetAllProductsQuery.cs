using ECommercePaymentIntegration.Application.Common;
using ECommercePaymentIntegration.Application.DTOs.Responses;

namespace ECommercePaymentIntegration.Application.Products.Queries.GetAllProducts;

public class GetAllProductsQuery : IRequest<IEnumerable<ProductResponse>>
{
}
