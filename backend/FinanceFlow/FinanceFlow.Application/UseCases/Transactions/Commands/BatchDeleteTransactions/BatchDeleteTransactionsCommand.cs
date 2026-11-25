using FinanceFlow.Application.DTOs;
using MediatR;

namespace FinanceFlow.Application.UseCases.Transactions.Commands.BatchDeleteTransactions;

/// <summary>
/// Exclui (soft delete) os lançamentos selecionados. Só os selecionados saem: o restante de uma recorrência ou
/// de uma compra parcelada é preservado. Transferências vinculadas têm o vínculo desfeito.
/// </summary>
public record BatchDeleteTransactionsCommand(Guid UserId, IReadOnlyList<Guid> Ids) : IRequest<BatchTransactionResultDto>;
