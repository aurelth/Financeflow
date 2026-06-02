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
            decimal receivedThisMonth;

            if (goal.LinkedCategoryId.HasValue)
            {
                // Progresso baseado nas transações vinculadas à categoria da meta
                accumulatedAmount = await transactionRepository
                    .GetTotalByCategoryAsync(goal.LinkedCategoryId.Value, cancellationToken);

                // Recebido = soma real das transações do mês atual (não o planejado)
                receivedThisMonth = await transactionRepository
                    .GetTotalByCategoryAndMonthAsync(
                        goal.LinkedCategoryId.Value,
                        now.Month,
                        now.Year,
                        cancellationToken);
            }
            else
            {
                // Comportamento legado
                accumulatedAmount = await CalculateLegacyProgressAsync(
                    goal, userId, today, cancellationToken);

                // Legado: recebido = planejado (comportamento original)
                receivedThisMonth = goal.MonthlyContribution;
            }

            var acc = Math.Round(accumulatedAmount, 2);
            var isCompleted = acc >= goal.TargetAmount;
            var progress = goal.TargetAmount > 0
                ? Math.Min(100, Math.Round((acc / goal.TargetAmount) * 100, 1))
                : 0;

            // Planejado = contribuição mensal definida pelo utilizador
            var plannedThisMonth = isCompleted ? 0 : goal.MonthlyContribution;

            // Recebido = 0 se meta concluída
            if (isCompleted) receivedThisMonth = 0;

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
                ReceivedThisMonth: Math.Round(receivedThisMonth, 2),
                ProgressPercentage: progress,
                IsCompleted: isCompleted,
                MonthsToComplete: monthsToComplete,
                Status: status,
                LinkedCategoryId: goal.LinkedCategoryId));
        }

        var totalCommittedNow = results.Where(r => !r.IsCompleted).Sum(r => r.PlannedThisMonth);
        var totalReceivedNow = results.Where(r => !r.IsCompleted).Sum(r => r.ReceivedThisMonth);
        var totalAccumulated = results.Sum(r => r.AccumulatedAmount);
        var difference = totalCommittedNow - totalReceivedNow;

        return new GoalsSummaryResultDto(
            AvailableThisMonth: Math.Round(totalAccumulated, 2),
            CommittedThisMonth: Math.Round(totalCommittedNow, 2),
            Difference: Math.Round(difference, 2),
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
