using System.Reflection;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using ECommercePaymentIntegration.API.Middleware;
using ECommercePaymentIntegration.Application.Common;
using ECommercePaymentIntegration.Infrastructure.DependencyInjection;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructureServices(builder.Configuration);

var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"];

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("ECommercePaymentIntegration.API"))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();

        if (!string.IsNullOrEmpty(otlpEndpoint))
            tracing.AddOtlpExporter(opts => opts.Endpoint = new Uri(otlpEndpoint));
        else
            tracing.AddConsoleExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();

        if (!string.IsNullOrEmpty(otlpEndpoint))
            metrics.AddOtlpExporter(opts => opts.Endpoint = new Uri(otlpEndpoint));
        else
            metrics.AddConsoleExporter();
    });

builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeScopes = true;
    logging.IncludeFormattedMessage = true;

    if (!string.IsNullOrEmpty(otlpEndpoint))
        logging.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
    else
        logging.AddConsoleExporter();
});

builder.Services.AddApplicationServices();


builder.Services.AddControllers();

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("fixed", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 10;
    });

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        var response = new
        {
            success = false,
            message = "Too many requests. Please try again later.",
            errors = (List<string>?)null
        };
        await context.HttpContext.Response.WriteAsJsonAsync(response, cancellationToken);
    };
});

// Response Compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "E-Commerce Payment Integration API",
        Version = "v1",
        Description = "E-Commerce backend API integrating with Balance Management service for payment processing. "
            + "This API provides endpoints for product listing, order creation with fund reservation, "
            + "and order completion/cancellation with finalized payments.",
        Contact = new OpenApiContact
        {
            Name = "API Support",
            Email = "support@ecommerce.com"
        },
        License = new OpenApiLicense
        {
            Name = "MIT",
        }
    });

    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("X-Correlation-Id");
    });
});

var app = builder.Build();


// Security headers first
app.UseSecurityHeaders();

// Correlation ID tracking
app.UseCorrelationId();

// Global exception handler
app.UseGlobalExceptionHandler();

// Response compression
app.UseResponseCompression();

// Swagger
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "E-Commerce API v1");
    options.RoutePrefix = "swagger";
});


app.UseCors();

app.UseRateLimiter();

app.MapHealthChecks("/health");

app.MapControllers().RequireRateLimiting("fixed");

app.Run();

public partial class Program { }
