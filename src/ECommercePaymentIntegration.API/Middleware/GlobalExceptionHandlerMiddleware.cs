using System.Net;
using System.Text.Json;
using ECommercePaymentIntegration.Domain.Exceptions;
using FluentValidation;
using ApplicationException = ECommercePaymentIntegration.Application.Exceptions.ApplicationException;

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

        var (statusCode, message, errors) = exception switch
        {
            ValidationException validationEx => (
                (int)HttpStatusCode.BadRequest,
                "Validation failed.",
                validationEx.Errors.Select(e => e.ErrorMessage).ToList()),
            DomainException domainEx => (domainEx.StatusCode, domainEx.Message, (List<string>?)null),
            ApplicationException appEx => (appEx.StatusCode, appEx.Message, (List<string>?)null),
            TaskCanceledException => ((int)HttpStatusCode.GatewayTimeout, "Request timed out.", (List<string>?)null),
            HttpRequestException => ((int)HttpStatusCode.BadGateway, "External service is unavailable.", (List<string>?)null),
            _ => ((int)HttpStatusCode.InternalServerError, "An unexpected error occurred.", (List<string>?)null)
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = JsonSerializer.Serialize(new { success = false, message, errors });
        await context.Response.WriteAsync(response);
    }
}

public static class GlobalExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }
}
