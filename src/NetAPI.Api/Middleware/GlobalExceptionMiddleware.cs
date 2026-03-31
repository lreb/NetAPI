using System.Net;
using System.Text.Json;
using NetAPI.Application.Exceptions;
using NetAPI.Domain.Exceptions;
using ValidationException = NetAPI.Application.Exceptions.ValidationException;

namespace NetAPI.Api.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, title, errors) = exception switch
        {
            ValidationException ve => (
                HttpStatusCode.UnprocessableEntity,
                "Validation Failed",
                ve.Errors),

            NotFoundException nfe => (
                HttpStatusCode.NotFound,
                nfe.Message,
                (IDictionary<string, string[]>?)null),

            DomainException de => (
                HttpStatusCode.BadRequest,
                de.Message,
                (IDictionary<string, string[]>?)null),

            _ => (
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred.",
                (IDictionary<string, string[]>?)null)
        };

        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            status = (int)statusCode,
            title,
            traceId = context.TraceIdentifier,
            errors
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}
