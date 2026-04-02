using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.API.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FinanceFlow.API.Services;

public class NotificationHubService(
    IHubContext<NotificationHub> hubContext
) : INotificationHubService
{
    public async Task SendNotificationAsync(
        Guid userId,
        string type,
        string message,
        CancellationToken cancellationToken = default)
    {
        await hubContext.Clients
            .Group(userId.ToString())
            .SendAsync("ReceiveNotification", new { type, message }, cancellationToken);
    }
}
