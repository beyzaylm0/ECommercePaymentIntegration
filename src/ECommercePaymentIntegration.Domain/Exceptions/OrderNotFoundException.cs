namespace ECommercePaymentIntegration.Domain.Exceptions;

public class OrderNotFoundException : DomainException
{
    public OrderNotFoundException(string orderId)
        : base($"Order with ID '{orderId}' was not found.", 404)
    {
    }
}
