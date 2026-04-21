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
    public async Task<GoalsSummaryResult> CalculateAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Calculando progresso das metas para utilizador {UserId}.", userId);

        var goals = (await goalRepository.GetByUserAsync(userId, cancellationToken)).ToList();

        if (goals.Count == 0)
            return new GoalsSummaryResult(0, 0, 0, []);

        var now = DateTime.UtcNow;
        var today = new DateTime(now.Year, now.Month, 1);

        // Calcula a poupança mensal desde a meta mais antiga até hoje
        var oldestCreation = goals.Min(g => g.CreatedAt);
        var monthlyBudgets = new Dictionary<(int Year, int Month), decimal>();

        var cursor = new DateTime(oldestCreation.Year, oldestCreation.Month, 1);
        while (cursor <= today)
        {
            // Execução sequencial — EF Core não suporta queries paralelas no mesmo DbContext
            var (income, expense, _) = await transactionRepository
                .GetMonthlySummaryAsync(userId, cursor.Month, cursor.Year, cancellationToken);

            monthlyBudgets[(cursor.Year, cursor.Month)] = Math.Max(0, income - expense);
            cursor = cursor.AddMonths(1);
        }

        // Acumula o progresso de cada meta mês a mês
        var accumulated = goals.ToDictionary(g => g.Id, _ => 0m);

        cursor = new DateTime(oldestCreation.Year, oldestCreation.Month, 1);
        while (cursor <= today)
        {
            var available = monthlyBudgets[(cursor.Year, cursor.Month)];

            // Metas ativas neste mês (criadas até este mês e não concluídas)
            var activeGoals = goals
                .Where(g =>
                    new DateTime(g.CreatedAt.Year, g.CreatedAt.Month, 1) <= cursor &&
                    accumulated[g.Id] < g.TargetAmount)
                .ToList();

            if (activeGoals.Count == 0 || available <= 0)
            {
                cursor = cursor.AddMonths(1);
                continue;
            }

            var totalCommitted = activeGoals.Sum(g => g.MonthlyContribution);

            if (available >= totalCommitted)
            {
                // Cada meta recebe o valor planeado
                foreach (var goal in activeGoals)
                {
                    var toAdd = Math.Min(
                        goal.MonthlyContribution,
                        goal.TargetAmount - accumulated[goal.Id]);
                    accumulated[goal.Id] += toAdd;
                }
            }
            else
            {
                // Distribuição proporcional
                foreach (var goal in activeGoals)
                {
                    var proportion = goal.MonthlyContribution / totalCommitted;
                    var toAdd = Math.Min(
                        available * proportion,
                        goal.TargetAmount - accumulated[goal.Id]);
                    accumulated[goal.Id] += toAdd;
                }
            }

            cursor = cursor.AddMonths(1);
        }

        // Calcula o mês atual
        var currentAvailable = monthlyBudgets[(today.Year, today.Month)];
        var currentActiveGoals = goals
            .Where(g =>
                new DateTime(g.CreatedAt.Year, g.CreatedAt.Month, 1) <= today &&
                accumulated[g.Id] < g.TargetAmount)
            .ToList();

        var totalCommittedNow = currentActiveGoals.Sum(g => g.MonthlyContribution);

        // Monta os resultados
        var results = goals.Select(goal =>
        {
            var acc = Math.Round(accumulated[goal.Id], 2);
            var isCompleted = acc >= goal.TargetAmount;
            var progress = goal.TargetAmount > 0
                ? Math.Min(100, Math.Round((acc / goal.TargetAmount) * 100, 1))
                : 0;

            // Contribuição planeada e recebida no mês atual
            decimal plannedThisMonth = 0;
            decimal receivedThisMonth = 0;

            if (!isCompleted && new DateTime(goal.CreatedAt.Year, goal.CreatedAt.Month, 1) <= today)
            {
                plannedThisMonth = goal.MonthlyContribution;

                if (currentAvailable >= totalCommittedNow)
                    receivedThisMonth = goal.MonthlyContribution;
                else if (totalCommittedNow > 0)
                    receivedThisMonth = Math.Round(
                        currentAvailable * (goal.MonthlyContribution / totalCommittedNow), 2);
            }

            // Projeção de conclusão
            int? monthsToComplete = null;
            if (!isCompleted && goal.MonthlyContribution > 0)
            {
                var remaining = goal.TargetAmount - acc;
                var avgContribution = receivedThisMonth > 0
                    ? receivedThisMonth
                    : goal.MonthlyContribution;
                monthsToComplete = (int)Math.Ceiling((double)(remaining / avgContribution));
            }

            // Status
            var status = isCompleted ? "Completed"
                : today > new DateTime(goal.Deadline.Year, goal.Deadline.Month, 1) ? "Overdue"
                : receivedThisMonth < plannedThisMonth ? "Behind"
                : "OnTrack";

            return new GoalProgressResult(
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
                Status: status);
        }).ToList();

        return new GoalsSummaryResult(
            AvailableThisMonth: Math.Round(currentAvailable, 2),
            CommittedThisMonth: Math.Round(totalCommittedNow, 2),
            Difference: Math.Round(currentAvailable - totalCommittedNow, 2),
            Goals: results);
    }
}
