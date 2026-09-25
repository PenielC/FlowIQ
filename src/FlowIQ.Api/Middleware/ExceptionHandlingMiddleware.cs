using System.Net;
using System.Text.Json;
using FlowIQ.Contracts.Common;
using FlowIQ.Domain.Exceptions;
using ValidationException = FlowIQ.Application.Common.Exceptions.ValidationException;

namespace FlowIQ.Api.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            await HandleExceptionAsync(context, exception);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, response) = exception switch
        {
            ValidationException validationException => (
                HttpStatusCode.BadRequest,
                ApiResponse<object>.Fail("Validation failed.", validationException.Errors)),
            DomainException domainException => (
                HttpStatusCode.BadRequest,
                ApiResponse<object>.Fail(domainException.Message)),
            KeyNotFoundException => (
                HttpStatusCode.NotFound,
                ApiResponse<object>.Fail("The requested resource was not found.")),
            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                ApiResponse<object>.Fail("Unauthorized.")),
            _ => (
                HttpStatusCode.InternalServerError,
                ApiResponse<object>.Fail("An unexpected error occurred.")),
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
