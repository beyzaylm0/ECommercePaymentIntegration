namespace ECommercePaymentIntegration.Domain.Exceptions;

public class InsufficientBalanceException : DomainException
{
    public InsufficientBalanceException()
        : base("Insufficient balance to complete the order.", 400)
    {
    }
}
