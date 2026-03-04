using System.Net;
using System.Text.Json;
using ECommercePaymentIntegration.Domain.Exceptions;

namespace ECommercePaymentIntegration.API.Middleware;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "An unhandled exception occurred");

        var (statusCode, message) = exception switch
        {
            DomainException domainEx => (domainEx.StatusCode, domainEx.Message),
            TaskCanceledException => ((int)HttpStatusCode.GatewayTimeout, "Request timed out."),
            HttpRequestException => ((int)HttpStatusCode.BadGateway, "External service is unavailable."),
            _ => ((int)HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = JsonSerializer.Serialize(new { success = false, message });
        await context.Response.WriteAsync(response);
    }
}
