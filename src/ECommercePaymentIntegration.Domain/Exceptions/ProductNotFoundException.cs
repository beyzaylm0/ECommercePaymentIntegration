namespace ECommercePaymentIntegration.Domain.Exceptions;

public class ProductNotFoundException : DomainException
{
    public ProductNotFoundException(string productId)
        : base($"Product with ID '{productId}' was not found.", 404)
    {
    }
}
