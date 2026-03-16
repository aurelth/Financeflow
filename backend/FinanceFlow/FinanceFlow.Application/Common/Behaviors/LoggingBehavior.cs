using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace FinanceFlow.Application.Common.Behaviors;

public class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        logger.LogInformation(
            "Iniciando request: {RequestName}",
            requestName);

        var sw = Stopwatch.StartNew();

        try
        {
            var response = await next();
            sw.Stop();

            logger.LogInformation(
                "Request concluído: {RequestName} em {ElapsedMs}ms",
                requestName,
                sw.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();

            logger.LogError(ex,
                "Request falhou: {RequestName} em {ElapsedMs}ms",
                requestName,
                sw.ElapsedMilliseconds);

            throw;
        }
    }
}
