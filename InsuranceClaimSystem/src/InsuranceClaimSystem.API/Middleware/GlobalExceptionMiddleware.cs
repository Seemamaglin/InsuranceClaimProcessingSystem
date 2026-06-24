using System.Net;
using System.Text.Json;
using InsuranceClaimSystem.Application.Common;

namespace InsuranceClaimSystem.API.Middleware;

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
            _logger.LogError(ex, "An unhandled exception occurred. CorrelationId: {CorrelationId}", 
                context.Items["CorrelationId"]?.ToString());
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var message = "An internal server error occurred.";
        var statusCode = (int)HttpStatusCode.InternalServerError;

        switch (exception)
        {
            case UnauthorizedAccessException:
                statusCode = (int)HttpStatusCode.Unauthorized;
                message = "Unauthorized access.";
                break;

            case ArgumentException argEx:
                statusCode = (int)HttpStatusCode.BadRequest;
                message = argEx.Message;
                break;

            case InvalidOperationException invOpEx:
                statusCode = (int)HttpStatusCode.BadRequest;
                message = invOpEx.Message;
                break;

            case KeyNotFoundException:
                statusCode = (int)HttpStatusCode.NotFound;
                message = "The requested resource was not found.";
                break;

            default:
                statusCode = (int)HttpStatusCode.InternalServerError;
                message = "An internal server error occurred. Please try again later.";
                break;
        }

        context.Response.StatusCode = statusCode;

        var response = ApiResponse<object>.Fail(message, new List<Error> { Error.Validation("UnhandledException", message) });

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}