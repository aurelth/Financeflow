using FinanceFlow.Application.DTOs.Onboarding;
using FinanceFlow.Application.UseCases.Onboarding.Commands.SetOnboardingChecklistDismissed;
using FinanceFlow.Application.UseCases.Onboarding.Queries.GetOnboardingChecklist;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceFlow.API.Controllers;

[Authorize]
public class OnboardingController(IMediator mediator) : BaseController(mediator)
{
    /// <summary>Progresso do checklist de primeiros passos, calculado a partir dos dados do usuário.</summary>
    [HttpGet("checklist")]
    [ProducesResponseType(typeof(OnboardingChecklistDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetChecklist(CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new GetOnboardingChecklistQuery(CurrentUserId), cancellationToken));

    /// <summary>Dispensa o checklist ou volta a mostrá-lo.</summary>
    [HttpPut("checklist/dismissed")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetDismissed(
        [FromBody] SetChecklistDismissedRequestDto request,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(
            new SetOnboardingChecklistDismissedCommand(CurrentUserId, request.Dismissed),
            cancellationToken);
        return NoContent();
    }
}
