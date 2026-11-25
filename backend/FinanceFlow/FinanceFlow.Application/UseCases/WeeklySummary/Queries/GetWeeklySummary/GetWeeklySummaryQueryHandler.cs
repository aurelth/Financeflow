using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.DTOs.WeeklySummary;
using FinanceFlow.Application.Services;
using FinanceFlow.Application.UseCases.Budgets.Queries.GetBudgetSummary;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.WeeklySummary.Queries.GetWeeklySummary;

public class GetWeeklySummaryQueryHandler(
    ITransactionRepository transactionRepository,
    IUserRepository userRepository,
    ISender sender,
    TimeProvider clock)
    : IRequestHandler<GetWeeklySummaryQuery, WeeklySummaryDto>
{
    /// <summary>Quantas categorias entram no ranking de gastos.</summary>
    public const int TopCategoriesCount = 3;

    /// <summary>Quantos vencimentos próximos são listados individualmente (os totais contam todos).</summary>
    public const int UpcomingItemsCount = 5;

    /// <summary>Orçamentos a partir deste percentual entram como alerta.</summary>
    public const decimal BudgetAlertPercentage = 80m;

    public const int BudgetAlertsCount = 3;

    /// <summary>Mesma janela do calendário: contas vencidas há mais que isso são histórico, não pendência.</summary>
    private const int OverdueLookbackDays = 90;
    private const int OverdueLimit = 100;
    private const int UpcomingDays = 7;

    public async Task<WeeklySummaryDto> Handle(GetWeeklySummaryQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("Usuário não encontrado.");

        var today = UserClock.LocalToday(user.Timezone, clock.GetUtcNow());
        var weekStart = WeeklySummaryCalendar.PreviousWeekStart(today);
        var weekEndExclusive = weekStart.AddDays(7);
        var previousStart = weekStart.AddDays(-7);

        // Uma consulta traz as duas semanas (a fechada e a anterior, para comparar)
        var twoWeeks = await transactionRepository.GetByPeriodAsync(
            request.UserId, previousStart, weekEndExclusive, cancellationToken);

        var week = Settled(twoWeeks, weekStart, weekEndExclusive);
        var previous = Settled(twoWeeks, previousStart, weekStart);

        var income = week.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var expense = week.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
        var previousIncome = previous.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var previousExpense = previous.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);

        var previousByCategory = previous
            .Where(t => t.Type == TransactionType.Expense)
            .GroupBy(t => t.CategoryId)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

        var topCategories = week
            .Where(t => t.Type == TransactionType.Expense)
            .GroupBy(t => t.CategoryId)
            .Select(g =>
            {
                var amount = g.Sum(t => t.Amount);
                return new WeeklyCategoryDto(
                    Name: g.First().Category.Name,
                    Icon: g.First().Category.Icon,
                    Amount: amount,
                    PreviousAmount: previousByCategory.GetValueOrDefault(g.Key, 0m),
                    Percentage: expense > 0 ? Math.Round(amount / expense * 100, 1) : 0m);
            })
            .OrderByDescending(c => c.Amount)
            .Take(TopCategoriesCount)
            .ToList();

        var upcomingTransactions = (await transactionRepository.GetByPeriodAsync(
                request.UserId, today, today.AddDays(UpcomingDays), cancellationToken))
            .Where(t => t.Status != TransactionStatus.Paid)
            .ToList();

        var upcoming = new WeeklyUpcomingDto(
            ExpenseCount: upcomingTransactions.Count(t => t.Type == TransactionType.Expense),
            ExpenseAmount: upcomingTransactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount),
            IncomeAmount: upcomingTransactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
            Items: upcomingTransactions
                .OrderBy(t => t.Date)
                .ThenByDescending(t => t.Amount)
                .Take(UpcomingItemsCount)
                .Select(t => new WeeklyUpcomingItemDto(t.Description, t.Amount, t.Date, t.Type))
                .ToList());

        var overdueExpenses = (await transactionRepository.GetUnpaidBeforeAsync(
                request.UserId, today, today.AddDays(-OverdueLookbackDays), OverdueLimit, cancellationToken))
            .Where(t => t.Type == TransactionType.Expense)
            .ToList();

        var overdue = new WeeklyOverdueDto(overdueExpenses.Count, overdueExpenses.Sum(t => t.Amount));

        var budgets = await sender.Send(
            new GetBudgetSummaryQuery(request.UserId, today.Month, today.Year), cancellationToken);

        var budgetAlerts = budgets
            .Where(b => b.Percentage >= BudgetAlertPercentage)
            .OrderByDescending(b => b.Percentage)
            .Take(BudgetAlertsCount)
            .Select(b => new WeeklyBudgetAlertDto(b.CategoryName, b.LimitAmount, b.SpentAmount, b.Percentage))
            .ToList();

        var hasActivity = week.Count > 0
            || upcoming.Items.Count > 0
            || overdue.Count > 0
            || budgetAlerts.Count > 0;

        return new WeeklySummaryDto(
            WeekStart: weekStart,
            WeekEnd: weekEndExclusive.AddDays(-1),
            Currency: string.IsNullOrWhiteSpace(user.Currency) ? "BRL" : user.Currency,
            Income: income,
            Expense: expense,
            Balance: income - expense,
            PreviousIncome: previousIncome,
            PreviousExpense: previousExpense,
            TopCategories: topCategories,
            Upcoming: upcoming,
            Overdue: overdue,
            BudgetAlerts: budgetAlerts,
            HasActivity: hasActivity);
    }

    /// <summary>Lançamentos já pagos (receitas e despesas) do intervalo [from, toExclusive).</summary>
    private static List<Transaction> Settled(IEnumerable<Transaction> transactions, DateTime from, DateTime toExclusive) =>
        transactions
            .Where(t =>
                t.Status == TransactionStatus.Paid &&
                t.Type != TransactionType.Transfer &&
                t.Date >= from &&
                t.Date < toExclusive)
            .ToList();
}
