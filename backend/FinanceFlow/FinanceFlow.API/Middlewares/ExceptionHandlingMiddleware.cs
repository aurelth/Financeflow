using System.Net;
using System.Text.Json;
using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Infrastructure.Localization;

namespace FinanceFlow.API.Middlewares;

public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly HashSet<string> Supported = ["pt-BR", "en-US", "es-ES", "fr-FR"];

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
        context.Response.ContentType = "application/json";

        // Detecta idioma do header
        var language = DetectLanguage(context);

        if (ex is ValidationException validationEx)
        {
            context.Response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;
            var validationResponse = JsonSerializer.Serialize(new
            {
                status = (int)HttpStatusCode.UnprocessableEntity,
                message = validationEx.Errors.Count == 0
                    ? validationEx.Message
                    : Messages.Get(Messages.ValidationError, language),
                errors = validationEx.Errors,
                traceId = context.TraceIdentifier,
            });
            return context.Response.WriteAsync(validationResponse);
        }

        var (statusCode, message) = ex switch
        {
            NotFoundException => (HttpStatusCode.NotFound, ex.Message),
            UnauthorizedException => (HttpStatusCode.Unauthorized, Messages.Get(Messages.Unauthorized, language)),
            _ => (HttpStatusCode.InternalServerError, Messages.Get(Messages.InternalError, language)),
        };

        context.Response.StatusCode = (int)statusCode;
        var response = JsonSerializer.Serialize(new
        {
            status = (int)statusCode,
            message,
            traceId = context.TraceIdentifier,
        });
        return context.Response.WriteAsync(response);
    }

    // Detecta idioma do header Accept-Language
    private static string DetectLanguage(HttpContext context)
    {
        var header = context.Request.Headers["Accept-Language"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(header)) return "en-US";
        var primary = header.Split(',')[0].Split(';')[0].Trim();
        return Supported.Contains(primary) ? primary : "en-US";
    }
}
