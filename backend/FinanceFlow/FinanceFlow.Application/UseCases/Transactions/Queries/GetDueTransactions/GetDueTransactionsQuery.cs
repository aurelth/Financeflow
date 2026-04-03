using FinanceFlow.Application.DTOs;
using MediatR;

namespace FinanceFlow.Application.UseCases.Transactions.Queries.GetDueTransactions;

public record GetDueTransactionsQuery(
    DateTime TargetDate
) : IRequest<IEnumerable<DueTransactionDto>>;
