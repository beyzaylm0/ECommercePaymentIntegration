namespace ECommercePaymentIntegration.Application.Exceptions;

public class ExternalServiceException : ApplicationException
{
    public ExternalServiceException(string serviceName, string message)
        : base($"External service '{serviceName}' error: {message}", 502)
    {
    }
}
