using FinanceFlow.Application.DTOs.Recurring;
using FinanceFlow.Application.Services;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Recurring.Queries.GetRecurringSuggestions;

public class GetRecurringSuggestionsQueryHandler(
    ITransactionRepository transactionRepository,
    IDismissedRecurrenceRepository dismissedRepository,
    TimeProvider clock)
    : IRequestHandler<GetRecurringSuggestionsQuery, RecurringSuggestionsDto>
{
    public async Task<RecurringSuggestionsDto> Handle(
        GetRecurringSuggestionsQuery request,
        CancellationToken cancellationToken)
    {
        var detected = await RecurringSuggestionLoader.LoadAsync(
            request.UserId, transactionRepository, dismissedRepository, clock, cancellationToken);

        var items = detected
            .Select(d =>
            {
                var latest = d.Latest;
                return new RecurringSuggestionDto(
                    Key: d.Key,
                    Type: d.Type,
                    Description: latest.Description,
                    Cadence: d.Cadence,
                    Kind: d.Kind.ToString(),
                    Amount: d.Amount,
                    AmountVaries: d.AmountVaries,
                    Occurrences: d.Occurrences.Count,
                    FirstDate: d.FirstDate,
                    LastDate: d.LastDate,
                    NextExpectedDate: d.NextExpectedDate,
                    MonthlyCost: d.MonthlyCost,
                    Confidence: d.Confidence,
                    CategoryId: latest.CategoryId,
                    CategoryName: latest.Category.Name,
                    CategoryIcon: latest.Category.Icon,
                    CategoryColor: latest.Category.Color,
                    AccountId: latest.AccountId,
                    AccountName: latest.Account.Name);
            })
            .ToList();

        return new RecurringSuggestionsDto(
            Items: items,
            MonthlyExpenseTotal: items.Where(i => i.Type == FinanceFlow.Domain.Entities.TransactionType.Expense).Sum(i => i.MonthlyCost),
            MonthlyIncomeTotal: items.Where(i => i.Type == FinanceFlow.Domain.Entities.TransactionType.Income).Sum(i => i.MonthlyCost));
    }
}
