using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Transactions.Queries.GetDueTransactions;

public class GetDueTransactionsQueryHandler(
    ITransactionRepository transactionRepository
) : IRequestHandler<GetDueTransactionsQuery, IEnumerable<DueTransactionDto>>
{
    public async Task<IEnumerable<DueTransactionDto>> Handle(
        GetDueTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        var transactions = await transactionRepository
            .GetDueTransactionsAsync(request.TargetDate, cancellationToken);

        return transactions.Select(t => new DueTransactionDto(
            Id: t.Id,
            UserId: t.UserId,
            Description: t.Description,
            Amount: t.Amount,
            Date: t.Date,
            IsRecurring: t.IsRecurring,
            RecurrenceType: t.RecurrenceType.ToString()
        ));
    }
}
