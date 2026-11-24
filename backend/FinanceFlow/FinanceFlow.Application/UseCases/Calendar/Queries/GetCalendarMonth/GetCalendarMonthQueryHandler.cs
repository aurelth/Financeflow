using FinanceFlow.Application.DTOs.Calendar;
using FinanceFlow.Application.Services;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Calendar.Queries.GetCalendarMonth;

public class GetCalendarMonthQueryHandler(
    ITransactionRepository transactionRepository,
    IUserRepository userRepository,
    TimeProvider clock)
    : IRequestHandler<GetCalendarMonthQuery, CalendarMonthDto>
{
    /// <summary>Quanto para trás procurar contas vencidas e não pagas: mais velhas que isso são histórico, não pendência.</summary>
    public const int OverdueLookbackDays = 90;

    /// <summary>Teto da lista de vencidas, para a tela continuar útil mesmo com um legado grande de agendados antigos.</summary>
    public const int OverdueLimit = 100;

    public async Task<CalendarMonthDto> Handle(GetCalendarMonthQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        var today = UserClock.LocalToday(user?.Timezone, clock.GetUtcNow());

        var from = new DateTime(request.Year, request.Month, 1);
        var toExclusive = from.AddMonths(1);

        var monthTransactions = await transactionRepository.GetByPeriodAsync(
            request.UserId, from, toExclusive, cancellationToken);

        var overdueTransactions = await transactionRepository.GetUnpaidBeforeAsync(
            request.UserId, today, today.AddDays(-OverdueLookbackDays), OverdueLimit, cancellationToken);

        var days = monthTransactions
            .GroupBy(t => t.Date.Date)
            .OrderBy(g => g.Key)
            .Select(g => new CalendarDayDto(
                Date: g.Key,
                Totals: Totals(g),
                Entries: g.Select(t => ToEntry(t, today)).ToList()))
            .ToList();

        var overdueEntries = overdueTransactions.Select(t => ToEntry(t, today)).ToList();

        return new CalendarMonthDto(
            Month: request.Month,
            Year: request.Year,
            Today: today,
            Days: days,
            Totals: Totals(monthTransactions),
            Overdue: new CalendarOverdueDto(
                Count: overdueEntries.Count,
                ExpenseAmount: overdueEntries.Where(e => e.Type == TransactionType.Expense).Sum(e => e.Amount),
                IncomeAmount: overdueEntries.Where(e => e.Type == TransactionType.Income).Sum(e => e.Amount),
                Entries: overdueEntries));
    }

    private static CalendarTotalsDto Totals(IEnumerable<Transaction> transactions)
    {
        decimal toPay = 0, toReceive = 0, paid = 0, received = 0;

        foreach (var t in transactions)
        {
            var settled = t.Status == TransactionStatus.Paid;

            if (t.Type == TransactionType.Expense)
            {
                if (settled) paid += t.Amount; else toPay += t.Amount;
            }
            else if (t.Type == TransactionType.Income)
            {
                if (settled) received += t.Amount; else toReceive += t.Amount;
            }
        }

        return new CalendarTotalsDto(toPay, toReceive, paid, received);
    }

    private static CalendarEntryDto ToEntry(Transaction t, DateTime today) =>
        new(
            Id: t.Id,
            Description: t.Description,
            Amount: t.Amount,
            Type: t.Type,
            Status: t.Status,
            Date: t.Date,
            IsRecurring: t.IsRecurring,
            InstallmentNumber: t.InstallmentNumber,
            InstallmentCount: t.InstallmentCount,
            CategoryId: t.CategoryId,
            CategoryName: t.Category.Name,
            CategoryIcon: t.Category.Icon,
            CategoryColor: t.Category.Color,
            AccountId: t.AccountId,
            AccountName: t.Account?.Name ?? string.Empty,
            IsOverdue: t.Status != TransactionStatus.Paid && t.Date.Date < today);
}
