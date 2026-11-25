using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Entities;
using MediatR;

namespace FinanceFlow.Application.UseCases.Transactions.Commands.BatchUpdateTransactions;

/// <summary>
/// Aplica categoria, conta e/ou status a vários lançamentos de uma vez. Um lançamento ao qual a mudança não se
/// aplica (tipo diferente da categoria, transferência vinculada...) fica de fora e volta na lista de ignorados.
/// </summary>
public record BatchUpdateTransactionsCommand(
    Guid UserId,
    IReadOnlyList<Guid> Ids,
    Guid? CategoryId,
    Guid? AccountId,
    TransactionStatus? Status) : IRequest<BatchTransactionResultDto>;
