using FinanceFlow.Application.DTOs.Assistant;
using FinanceFlow.Application.UseCases.Assistant.Commands.SendMessage;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceFlow.API.Controllers;

[Authorize]
public class AssistantController(IMediator mediator) : BaseController(mediator)
{
    /// <summary>Envia uma mensagem ao assistente financeiro IA.</summary>
    [HttpPost("chat")]
    [ProducesResponseType(typeof(SendMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Chat(
        [FromBody] SendMessageRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new SendMessageCommand(
            UserId: CurrentUserId,
            Message: request.Message);

        var result = await Mediator.Send(command, cancellationToken);
        return Ok(result);
    }
}
