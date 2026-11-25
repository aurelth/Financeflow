using System.Globalization;
using FinanceFlow.Application.DTOs.WeeklySummary;

namespace FinanceFlow.Application.Services;

/// <summary>Textos do resumo semanal (notificação, assunto e valores do e-mail), em pt-BR.</summary>
public static class WeeklySummaryText
{
    private static readonly CultureInfo Culture = new("pt-BR");

    /// <summary>Limite da mensagem de notificação (a API recusa mais que isso).</summary>
    public const int MaxMessageLength = 500;

    public static string Money(decimal amount, string currency)
    {
        var number = Math.Abs(amount).ToString("N2", Culture);
        var formatted = currency.Equals("BRL", StringComparison.OrdinalIgnoreCase)
            ? $"R$ {number}"
            : $"{number} {currency.ToUpperInvariant()}";

        return amount < 0 ? $"-{formatted}" : formatted;
    }

    public static string Period(WeeklySummaryDto summary) =>
        $"{summary.WeekStart:dd/MM} a {summary.WeekEnd:dd/MM}";

    public static string Subject(WeeklySummaryDto summary) =>
        $"Seu resumo da semana ({Period(summary)}) — FinanceFlow";

    /// <summary>"+12%" / "-8%" em relação à semana anterior; nulo quando não há base de comparação.</summary>
    public static string? Variation(decimal current, decimal previous)
    {
        if (previous <= 0)
            return null;

        var percent = Math.Round((current - previous) / previous * 100, 0);
        return percent switch
        {
            > 0 => $"+{percent:0}%",
            < 0 => $"{percent:0}%",
            _ => "0%",
        };
    }

    public static string BuildNotificationMessage(WeeklySummaryDto s)
    {
        var parts = new List<string>
        {
            $"📊 Resumo da semana ({Period(s)}): entrou {Money(s.Income, s.Currency)}, " +
            $"saiu {Money(s.Expense, s.Currency)} — saldo {Money(s.Balance, s.Currency)}."
        };

        if (s.TopCategories.Count > 0)
        {
            var top = s.TopCategories[0];
            parts.Add($"Maior gasto: {top.Name} ({Money(top.Amount, s.Currency)}).");
        }

        if (s.Upcoming.ExpenseCount > 0)
        {
            var plural = s.Upcoming.ExpenseCount == 1 ? "conta a pagar" : "contas a pagar";
            parts.Add($"{s.Upcoming.ExpenseCount} {plural} nos próximos 7 dias ({Money(s.Upcoming.ExpenseAmount, s.Currency)}).");
        }

        if (s.Overdue.Count > 0)
        {
            var plural = s.Overdue.Count == 1 ? "conta vencida" : "contas vencidas";
            parts.Add($"{s.Overdue.Count} {plural} ({Money(s.Overdue.Amount, s.Currency)}).");
        }

        if (s.BudgetAlerts.Count > 0)
        {
            var worst = s.BudgetAlerts[0];
            parts.Add($"Orçamento de {worst.CategoryName} em {worst.Percentage:0}%.");
        }

        var message = string.Join(' ', parts);

        return message.Length <= MaxMessageLength
            ? message
            : message[..(MaxMessageLength - 1)] + "…";
    }
}
