using FinanceFlow.Application.DTOs.Transfers;
using FinanceFlow.Application.UseCases.Transfers.Commands.CreateTransfer;
using FinanceFlow.Application.UseCases.Transfers.Commands.LinkTransfers;
using FinanceFlow.Application.UseCases.Transfers.Commands.UnlinkTransfer;
using FinanceFlow.Application.UseCases.Transfers.Queries.GetPendingTransfers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceFlow.API.Controllers;

[Authorize]
public class TransfersController(IMediator mediator) : BaseController(mediator)
{
    /// <summary>Cria uma transferência entre duas contas: saída na origem, entrada no destino e o vínculo.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(TransferLinkDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateTransferRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new CreateTransferCommand(
            UserId: CurrentUserId,
            FromAccountId: request.FromAccountId,
            ToAccountId: request.ToAccountId,
            Amount: request.Amount,
            Date: request.Date,
            Description: request.Description);

        var result = await Mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetPending), null, result);
    }

    /// <summary>Une dois lançamentos existentes (saída de uma conta e entrada em outra) numa transferência.</summary>
    [HttpPost("link")]
    [ProducesResponseType(typeof(TransferLinkDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Link(
        [FromBody] LinkTransfersRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new LinkTransfersCommand(
            CurrentUserId, request.FirstTransactionId, request.SecondTransactionId);

        var result = await Mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>Desfaz uma transferência: devolve as pontas como despesa/receita ou exclui as duas.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Unlink(
        Guid id,
        [FromBody] UnlinkTransferRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new UnlinkTransferCommand(
            CurrentUserId, id, request.OutCategoryId, request.InCategoryId, request.DeleteTransactions);

        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>Pontas de transferência sem par, com a melhor sugestão de par para cada uma.</summary>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(IEnumerable<PendingTransferDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPending(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetPendingTransfersQuery(CurrentUserId), cancellationToken);
        return Ok(result);
    }
}
