namespace ECommercePaymentIntegration.Application.Exceptions;

public class ApplicationException : Exception
{
    public int StatusCode { get; }

    public ApplicationException(string message, int statusCode = 500) : base(message)
    {
        StatusCode = statusCode;
    }
}
