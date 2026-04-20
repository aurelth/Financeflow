using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.DTOs.HealthScore;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace FinanceFlow.Infrastructure.AI;

public class HealthScoreService(
    ITransactionRepository transactionRepository,
    IBudgetRepository budgetRepository,
    ILogger<HealthScoreService> logger) : IHealthScoreService
{
    public async Task<HealthScoreResult> CalculateAsync(
        Guid userId,
        int month,
        int year,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Calculando score de saúde financeira para utilizador {UserId} — {Month}/{Year}.", userId, month, year);

        // Execução sequencial — EF Core não suporta queries paralelas no mesmo DbContext
        var (totalIncome, totalExpense, transactions) =
            await transactionRepository.GetMonthlySummaryAsync(userId, month, year, cancellationToken);

        var topCategories =
            await transactionRepository.GetTopExpenseCategoriesAsync(userId, month, year, 10, cancellationToken);

        var budgets =
            await budgetRepository.GetByUserAndPeriodAsync(userId, month, year, cancellationToken);

        var transactionsList = transactions.ToList();
        var categoriesList = topCategories.ToList();
        var budgetsList = budgets.ToList();

        var details = new List<ScoreDetail>();

        // Critério 1 — Saldo positivo (25 pts)
        details.Add(CalculateSaldoPositivo(totalIncome, totalExpense));

        // Critério 2 — Controlo de orçamentos (25 pts)
        details.Add(CalculateControlOrcamentos(budgetsList, categoriesList));

        // Critério 3 — Regularidade de receitas (20 pts)
        details.Add(CalculateRegularidadeReceitas(totalIncome));

        // Critério 4 — Diversificação de despesas (15 pts)
        details.Add(CalculateDiversificacaoDespesas(totalExpense, categoriesList));

        // Critério 5 — Transações agendadas em dia (15 pts)
        details.Add(CalculateTransacoesAgendadas(transactionsList, month, year));

        var score = details.Sum(d => d.Points);
        var classification = ClassifyScore(score);

        return new HealthScoreResult(score, classification, details);
    }

    private static ScoreDetail CalculateSaldoPositivo(decimal totalIncome, decimal totalExpense)
    {
        const int maxPoints = 25;

        if (totalIncome == 0 && totalExpense == 0)
            return new ScoreDetail("Saldo do mês", 0, maxPoints,
                "Nenhuma movimentação registada este mês.");

        if (totalIncome == 0)
            return new ScoreDetail("Saldo do mês", 0, maxPoints,
                "Nenhuma receita registada. Sem receitas não é possível avaliar o saldo.");

        var balance = totalIncome - totalExpense;

        if (balance <= 0)
            return new ScoreDetail("Saldo do mês", 0, maxPoints,
                $"Saldo negativo de R$ {Math.Abs(balance):N2}. As despesas superaram as receitas.");

        // Proporção poupada: quanto mais poupa, mais pontos
        var savingsRate = balance / totalIncome;

        var points = savingsRate switch
        {
            >= 0.30m => maxPoints,           // Poupou 30%+ → pontuação máxima
            >= 0.20m => (int)(maxPoints * 0.8),  // Poupou 20-29%
            >= 0.10m => (int)(maxPoints * 0.6),  // Poupou 10-19%
            _ => (int)(maxPoints * 0.3),  // Poupou menos de 10%
        };

        var justification = savingsRate >= 0.30m
            ? $"Excelente! Poupou {savingsRate:P0} da receita (R$ {balance:N2})."
            : $"Saldo positivo de R$ {balance:N2}, mas há espaço para poupar mais ({savingsRate:P0} da receita).";

        return new ScoreDetail("Saldo do mês", points, maxPoints, justification);
    }

    private static ScoreDetail CalculateControlOrcamentos(
        IList<Budget> budgetsList,
        IList<(string CategoryName, decimal TotalAmount)> categoriesList)
    {
        const int maxPoints = 25;

        if (budgetsList.Count == 0)
            return new ScoreDetail("Controlo de orçamentos", 0, maxPoints,
                "Nenhum orçamento configurado. Defina limites por categoria para controlar melhor os gastos.");

        var total = budgetsList.Count;
        var dentroLimite = 0;

        foreach (var budget in budgetsList)
        {
            var spent = categoriesList
                .FirstOrDefault(c => c.CategoryName == budget.Category?.Name)
                .TotalAmount;

            if (spent <= budget.LimitAmount)
                dentroLimite++;
        }

        var percentage = (decimal)dentroLimite / total;
        var points = (int)(maxPoints * percentage);

        var justification = dentroLimite == total
            ? $"Perfeito! Todas as {total} categorias com orçamento estão dentro do limite."
            : $"{dentroLimite} de {total} categorias dentro do limite. {total - dentroLimite} categoria(s) excedida(s).";

        return new ScoreDetail("Controlo de orçamentos", points, maxPoints, justification);
    }

    private static ScoreDetail CalculateRegularidadeReceitas(decimal totalIncome)
    {
        const int maxPoints = 20;

        if (totalIncome == 0)
            return new ScoreDetail("Regularidade de receitas", 0, maxPoints,
                "Nenhuma receita registada este mês.");

        if (totalIncome >= 1000)
            return new ScoreDetail("Regularidade de receitas", maxPoints, maxPoints,
                $"Receitas de R$ {totalIncome:N2} registadas no mês.");

        // Receita baixa — pontuação parcial
        var points = (int)(maxPoints * 0.5);
        return new ScoreDetail("Regularidade de receitas", points, maxPoints,
            $"Receitas baixas (R$ {totalIncome:N2}). Registe todas as suas fontes de rendimento.");
    }

    private static ScoreDetail CalculateDiversificacaoDespesas(
        decimal totalExpense,
        IList<(string CategoryName, decimal TotalAmount)> categoriesList)
    {
        const int maxPoints = 15;

        if (totalExpense == 0 || categoriesList.Count == 0)
            return new ScoreDetail("Diversificação de despesas", maxPoints, maxPoints,
                "Nenhuma despesa registada este mês.");

        var topCategory = categoriesList.First();
        var topPercentage = topCategory.TotalAmount / totalExpense;

        if (topPercentage > 0.60m)
            return new ScoreDetail("Diversificação de despesas", 0, maxPoints,
                $"A categoria '{topCategory.CategoryName}' representa {topPercentage:P0} dos gastos. Gastos muito concentrados numa única categoria.");

        if (topPercentage > 0.45m)
        {
            var points = (int)(maxPoints * 0.5);
            return new ScoreDetail("Diversificação de despesas", points, maxPoints,
                $"A categoria '{topCategory.CategoryName}' representa {topPercentage:P0} dos gastos. Moderadamente concentrado.");
        }

        return new ScoreDetail("Diversificação de despesas", maxPoints, maxPoints,
            "Gastos bem distribuídos entre categorias.");
    }

    private static ScoreDetail CalculateTransacoesAgendadas(
        IList<Domain.Entities.Transaction> transactions,
        int month,
        int year)
    {
        const int maxPoints = 15;

        var hoje = new DateTime(year, month, DateTime.DaysInMonth(year, month));

        var agendadas = transactions
            .Where(t =>
                t.Status == TransactionStatus.Scheduled &&
                t.Date <= hoje)
            .ToList();

        if (agendadas.Count == 0)
            return new ScoreDetail("Transações agendadas", maxPoints, maxPoints,
                "Nenhuma transação agendada em atraso.");

        var emAtraso = agendadas
            .Where(t => t.Status == TransactionStatus.Scheduled)
            .ToList();

        if (emAtraso.Count == 0)
            return new ScoreDetail("Transações agendadas", maxPoints, maxPoints,
                "Todas as transações agendadas foram pagas em dia.");

        var percentage = (decimal)(agendadas.Count - emAtraso.Count) / agendadas.Count;
        var points = (int)(maxPoints * percentage);

        return new ScoreDetail("Transações agendadas", points, maxPoints,
            $"{emAtraso.Count} transação(ões) agendada(s) ainda não pagas este mês.");
    }

    private static string ClassifyScore(int score) => score switch
    {
        >= 80 => "Excelente",
        >= 60 => "Bom",
        >= 40 => "Regular",
        >= 20 => "Atenção",
        _ => "Crítico"
    };
}
