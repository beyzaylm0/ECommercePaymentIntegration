namespace ECommercePaymentIntegration.Domain.Exceptions;

public class ExternalServiceException : DomainException
{
    public ExternalServiceException(string serviceName, string message)
        : base($"External service '{serviceName}' error: {message}", 502)
    {
    }
}
