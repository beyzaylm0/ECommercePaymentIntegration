namespace ECommercePaymentIntegration.Domain.Exceptions;

public class InsufficientStockException : DomainException
{
    public InsufficientStockException(string productId, int requested, int available)
        : base($"Insufficient stock for product '{productId}'. Requested: {requested}, Available: {available}.", 400)
    {
    }
}
