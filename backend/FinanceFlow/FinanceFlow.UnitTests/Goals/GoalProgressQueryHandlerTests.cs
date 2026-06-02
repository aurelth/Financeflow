using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.DTOs.Goals;
using FinanceFlow.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace FinanceFlow.Application.Services;

public class GoalProgressService(
    IGoalRepository goalRepository,
    ITransactionRepository transactionRepository,
    ILogger<GoalProgressService> logger) : IGoalProgressService
{
    public async Task<GoalsSummaryResultDto> CalculateAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Calculando progresso das metas para utilizador {UserId}.", userId);

        var goals = (await goalRepository.GetByUserAsync(userId, cancellationToken)).ToList();

        if (goals.Count == 0)
            return new GoalsSummaryResultDto(0, 0, 0, []);

        var now = DateTime.UtcNow;
        var today = new DateTime(now.Year, now.Month, 1);

        var results = new List<GoalProgressResultDto>();

        foreach (var goal in goals)
        {
            decimal accumulatedAmount;
            decimal plannedThisMonth;
            decimal receivedThisMonth;

            if (goal.LinkedCategoryId.HasValue)
            {
                // Progresso baseado nas transações vinculadas à categoria da meta
                accumulatedAmount = await transactionRepository
                    .GetTotalByCategoryAsync(goal.LinkedCategoryId.Value, cancellationToken);

                var acc = Math.Round(accumulatedAmount, 2);
                var isCompleted = acc >= goal.TargetAmount;

                plannedThisMonth = isCompleted ? 0 : goal.MonthlyContribution;
                
                receivedThisMonth = isCompleted ? 0 : await transactionRepository
                    .GetTotalByCategoryAndMonthAsync(
                        goal.LinkedCategoryId.Value,
                        now.Month,
                        now.Year,
                        cancellationToken);

                var progress = goal.TargetAmount > 0
                    ? Math.Min(100, Math.Round((acc / goal.TargetAmount) * 100, 1))
                    : 0;

                int? monthsToComplete = null;
                if (!isCompleted && goal.MonthlyContribution > 0)
                {
                    var remaining = goal.TargetAmount - acc;
                    monthsToComplete = (int)Math.Ceiling((double)(remaining / goal.MonthlyContribution));
                }

                var status = isCompleted ? "Completed"
                    : today > new DateTime(goal.Deadline.Year, goal.Deadline.Month, 1) ? "Overdue"
                    : "OnTrack";

                results.Add(new GoalProgressResultDto(
                    Id: goal.Id,
                    Name: goal.Name,
                    Emoji: goal.Emoji,
                    TargetAmount: goal.TargetAmount,
                    MonthlyContribution: goal.MonthlyContribution,
                    Deadline: goal.Deadline,
                    AccumulatedAmount: acc,
                    PlannedThisMonth: plannedThisMonth,
                    ReceivedThisMonth: receivedThisMonth,
                    ProgressPercentage: progress,
                    IsCompleted: isCompleted,
                    MonthsToComplete: monthsToComplete,
                    Status: status,
                    LinkedCategoryId: goal.LinkedCategoryId));
            }
            else
            {
                // Comportamento legado para metas sem categoria vinculada
                accumulatedAmount = await CalculateLegacyProgressAsync(
                    goal, userId, today, cancellationToken);

                var acc = Math.Round(accumulatedAmount, 2);
                var isCompleted = acc >= goal.TargetAmount;
                var progress = goal.TargetAmount > 0
                    ? Math.Min(100, Math.Round((acc / goal.TargetAmount) * 100, 1))
                    : 0;

                plannedThisMonth = isCompleted ? 0 : goal.MonthlyContribution;
                receivedThisMonth = plannedThisMonth;

                int? monthsToComplete = null;
                if (!isCompleted && goal.MonthlyContribution > 0)
                {
                    var remaining = goal.TargetAmount - acc;
                    monthsToComplete = (int)Math.Ceiling((double)(remaining / goal.MonthlyContribution));
                }

                var status = isCompleted ? "Completed"
                    : today > new DateTime(goal.Deadline.Year, goal.Deadline.Month, 1) ? "Overdue"
                    : "OnTrack";

                results.Add(new GoalProgressResultDto(
                    Id: goal.Id,
                    Name: goal.Name,
                    Emoji: goal.Emoji,
                    TargetAmount: goal.TargetAmount,
                    MonthlyContribution: goal.MonthlyContribution,
                    Deadline: goal.Deadline,
                    AccumulatedAmount: acc,
                    PlannedThisMonth: plannedThisMonth,
                    ReceivedThisMonth: receivedThisMonth,
                    ProgressPercentage: progress,
                    IsCompleted: isCompleted,
                    MonthsToComplete: monthsToComplete,
                    Status: status,
                    LinkedCategoryId: goal.LinkedCategoryId));
            }
        }

        var totalCommittedNow = results.Where(r => !r.IsCompleted).Sum(r => r.PlannedThisMonth);
        var currentAvailable = 0m;

        return new GoalsSummaryResultDto(
            AvailableThisMonth: Math.Round(currentAvailable, 2),
            CommittedThisMonth: Math.Round(totalCommittedNow, 2),
            Difference: Math.Round(currentAvailable - totalCommittedNow, 2),
            Goals: results);
    }

    private async Task<decimal> CalculateLegacyProgressAsync(
        FinanceFlow.Domain.Entities.Goal goal,
        Guid userId,
        DateTime today,
        CancellationToken cancellationToken)
    {
        var oldestCreation = goal.CreatedAt;
        var accumulated = 0m;

        var cursor = new DateTime(oldestCreation.Year, oldestCreation.Month, 1);
        while (cursor <= today)
        {
            var (income, expense, _) = await transactionRepository
                .GetMonthlySummaryAsync(userId, cursor.Month, cursor.Year, cancellationToken);

            var available = Math.Max(0, income - expense);
            if (available > 0 && accumulated < goal.TargetAmount)
            {
                var toAdd = Math.Min(
                    goal.MonthlyContribution,
                    goal.TargetAmount - accumulated);
                accumulated += toAdd;
            }

            cursor = cursor.AddMonths(1);
        }

        return accumulated;
    }
}
