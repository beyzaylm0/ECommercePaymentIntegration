namespace ECommercePaymentIntegration.Domain.Exceptions;

public class DuplicateProductException : DomainException
{
    public DuplicateProductException(string productId)
        : base($"Duplicate product '{productId}' in order.", 400)
    {
    }
}
