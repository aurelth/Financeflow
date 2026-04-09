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
    // POST api/settings/logout-all
    [HttpPost("logout-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")!);

        await mediator.Send(new LogoutAllCommand(userId), cancellationToken);

        return NoContent();
    }
}
