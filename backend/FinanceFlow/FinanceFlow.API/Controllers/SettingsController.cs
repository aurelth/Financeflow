using FinanceFlow.Application.UseCases.Settings.Commands.DeleteAccount;
using FinanceFlow.Application.UseCases.Settings.Commands.LogoutAll;
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
        [FromBody] DeleteAccountRequest request,
        CancellationToken cancellationToken)
    {
        await mediator.Send(
            new DeleteAccountCommand(GetUserId(), request.CurrentPassword),
            cancellationToken);

        return NoContent();
    }
}

// DTO inline para o body do request
public record DeleteAccountRequest(string CurrentPassword);
