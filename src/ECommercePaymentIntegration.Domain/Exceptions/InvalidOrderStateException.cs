using ECommercePaymentIntegration.Domain.Enums;

namespace ECommercePaymentIntegration.Domain.Exceptions;

public class InvalidOrderStateException : DomainException
{
    public InvalidOrderStateException(string orderId, OrderStatus currentStatus, OrderStatus targetStatus)
        : base($"Order '{orderId}' cannot transition from '{currentStatus}' to '{targetStatus}'.", 400)
    {
    }
}
