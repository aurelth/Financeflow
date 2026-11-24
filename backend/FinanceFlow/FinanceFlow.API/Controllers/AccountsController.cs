using FinanceFlow.Application.DTOs.Accounts;
using FinanceFlow.Application.UseCases.Accounts.Commands.ArchiveAccount;
using FinanceFlow.Application.UseCases.Accounts.Commands.CreateAccount;
using FinanceFlow.Application.UseCases.Accounts.Commands.UpdateAccount;
using FinanceFlow.Application.UseCases.Accounts.Queries.GetAccountReconciliation;
using FinanceFlow.Application.UseCases.Accounts.Queries.GetAccounts;
using FinanceFlow.Application.UseCases.Accounts.Queries.GetAccountsReconciliation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceFlow.API.Controllers;

[Authorize]
public class AccountsController(IMediator mediator) : BaseController(mediator)
{
    /// <summary>Lista as contas do usuário com o saldo calculado.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AccountDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool includeArchived,
        CancellationToken cancellationToken)
    {
        var query = new GetAccountsQuery(CurrentUserId, includeArchived);
        var result = await Mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Concilia todas as contas ativas com o saldo do último extrato confirmado de cada uma.</summary>
    [HttpGet("reconciliation")]
    [ProducesResponseType(typeof(IEnumerable<AccountReconciliationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllReconciliation(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetAccountsReconciliationQuery(CurrentUserId), cancellationToken);
        return Ok(result);
    }

    /// <summary>Compara o saldo calculado da conta com o saldo do último extrato confirmado.</summary>
    [HttpGet("{id:guid}/reconciliation")]
    [ProducesResponseType(typeof(AccountReconciliationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReconciliation(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetAccountReconciliationQuery(id, CurrentUserId), cancellationToken);
        return Ok(result);
    }

    /// <summary>Cria uma conta.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateAccountRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new CreateAccountCommand(
            UserId: CurrentUserId,
            Name: request.Name,
            Type: request.Type,
            BankName: request.BankName,
            InitialBalance: request.InitialBalance,
            InitialBalanceDate: request.InitialBalanceDate,
            Currency: request.Currency,
            Color: request.Color,
            Icon: request.Icon);

        var result = await Mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetAll), result);
    }

    /// <summary>Atualiza (ou reativa) uma conta.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateAccountRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateAccountCommand(
            Id: id,
            UserId: CurrentUserId,
            Name: request.Name,
            Type: request.Type,
            BankName: request.BankName,
            InitialBalance: request.InitialBalance,
            InitialBalanceDate: request.InitialBalanceDate,
            Currency: request.Currency,
            Color: request.Color,
            Icon: request.Icon,
            IsArchived: request.IsArchived);

        var result = await Mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>Arquiva uma conta (preserva o histórico). A conta principal não pode ser arquivada.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Archive(
        Guid id,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(new ArchiveAccountCommand(id, CurrentUserId), cancellationToken);
        return NoContent();
    }
}
