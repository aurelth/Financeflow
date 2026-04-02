using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Application.UseCases.Notifications.Commands.CreateNotification;
using FinanceFlow.Application.UseCases.Notifications.Commands.MarkAllNotificationsAsRead;
using FinanceFlow.Application.UseCases.Notifications.Commands.MarkNotificationAsRead;
using FinanceFlow.Application.UseCases.Notifications.Queries.GetNotifications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceFlow.API.Controllers;

[Authorize]
public class NotificationsController(
    IMediator mediator,
    INotificationHubService notificationHubService)
    : BaseController(mediator)
{
    /// <summary>Lista todas as notificações do usuário autenticado.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var query = new GetNotificationsQuery(CurrentUserId);
        var result = await Mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Cria e despacha uma notificação (uso interno do Worker).</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateNotificationCommand command,
        CancellationToken cancellationToken)
    {
        var id = await Mediator.Send(command, cancellationToken);

        // Dispara em tempo real via SignalR
        await notificationHubService.SendNotificationAsync(
            command.UserId,
            command.Type,
            command.Message,
            cancellationToken);

        return CreatedAtAction(nameof(GetAll), new { id }, null);
    }

    /// <summary>Marca uma notificação como lida.</summary>
    [HttpPatch("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new MarkNotificationAsReadCommand(id, CurrentUserId);
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>Marca todas as notificações do usuário como lidas.</summary>
    [HttpPatch("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        var command = new MarkAllNotificationsAsReadCommand(CurrentUserId);
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
