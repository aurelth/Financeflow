using System.Net;
using System.Text.Json;
using FinanceFlow.Application.Common.Exceptions;

namespace FinanceFlow.API.Middlewares;

public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (statusCode, message) = ex switch
        {
            NotFoundException => (HttpStatusCode.NotFound, ex.Message),
            ValidationException => (HttpStatusCode.UnprocessableEntity, ex.Message),
            UnauthorizedException => (HttpStatusCode.Unauthorized, ex.Message),
            _ => (HttpStatusCode.InternalServerError, "Ocorreu um erro interno. Tente novamente.")
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = JsonSerializer.Serialize(new
        {
            status = (int)statusCode,
            message,
            traceId = context.TraceIdentifier
        });

        return context.Response.WriteAsync(response);
    }
}
