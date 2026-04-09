using FinanceFlow.Application.DTOs;
using FinanceFlow.Application.UseCases.Settings.Commands.DeleteAccount;
using FinanceFlow.Application.UseCases.Settings.Commands.LogoutAll;
using FinanceFlow.Application.UseCases.Settings.Commands.UpdateNotificationPreferences;
using FinanceFlow.Application.UseCases.Settings.Queries.GetNotificationPreferences;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinanceFlow.API.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize]
public class SettingsController(IMediator mediator) : ControllerBase
{
    private Guid GetUserId() => Guid.Parse(
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")!);

    // GET api/settings/notifications
    [HttpGet("notifications")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotificationPreferences(
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetNotificationPreferencesQuery(GetUserId()), cancellationToken);
        return Ok(result);
    }

    // PUT api/settings/notifications
    [HttpPut("notifications")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateNotificationPreferences(
        [FromBody] UpdateNotificationPreferencesRequestDto request,
        CancellationToken cancellationToken)
    {
        await mediator.Send(new UpdateNotificationPreferencesCommand(
            UserId: GetUserId(),
            BudgetWarningEnabled: request.BudgetWarningEnabled,
            BudgetCriticalEnabled: request.BudgetCriticalEnabled,
            TransactionDueTomorrowEnabled: request.TransactionDueTomorrowEnabled,
            TransactionDueIn3DaysEnabled: request.TransactionDueIn3DaysEnabled
        ), cancellationToken);

        return NoContent();
    }

    // POST api/settings/logout-all
    [HttpPost("logout-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
    {
        await mediator.Send(new LogoutAllCommand(GetUserId()), cancellationToken);
        return NoContent();
    }

    // DELETE api/settings/account
    [HttpDelete("account")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteAccount(
        [FromBody] DeleteAccountRequestDto request,
        CancellationToken cancellationToken)
    {
        await mediator.Send(
            new DeleteAccountCommand(GetUserId(), request.CurrentPassword),
            cancellationToken);
        return NoContent();
    }
}
