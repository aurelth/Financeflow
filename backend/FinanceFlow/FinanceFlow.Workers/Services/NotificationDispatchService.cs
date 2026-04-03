using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinanceFlow.Workers.Models;

namespace FinanceFlow.Workers.Services;

public class NotificationDispatchService(
    IHttpClientFactory httpClientFactory,
    ApiAuthService authService,
    ILogger<NotificationDispatchService> logger)
{
    public async Task ProcessAsync(
        NotificationEvent notification,
        CancellationToken cancellationToken)
    {
        var token = await authService.GetTokenAsync(cancellationToken);
        var client = httpClientFactory.CreateClient("FinanceFlowApi");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Persiste a notificação via API (que também dispara o SignalR)
        var response = await client.PostAsJsonAsync(
            "api/notifications",
            new
            {
                userId = notification.UserId,
                type = notification.Type,
                message = notification.Message,
                referenceId = notification.ReferenceId
            },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Erro ao persistir notificação para UserId {UserId}: {Status}",
                notification.UserId, response.StatusCode);
            return;
        }

        logger.LogInformation(
            "Notificação [{Type}] despachada para UserId {UserId}",
            notification.Type, notification.UserId);
    }
}
