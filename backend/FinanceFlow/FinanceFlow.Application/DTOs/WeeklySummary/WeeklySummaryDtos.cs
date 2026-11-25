using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Application.DTOs.WeeklySummary;

/// <summary>
/// Retrato da última semana fechada (segunda a domingo) do usuário, com o que vem pela frente.
/// Receitas e despesas contam só o que foi efetivamente pago; o que ainda está em aberto aparece em
/// <see cref="Upcoming"/> e <see cref="Overdue"/>.
/// </summary>
public record WeeklySummaryDto(
    DateTime WeekStart,
    DateTime WeekEnd,
    string Currency,
    decimal Income,
    decimal Expense,
    decimal Balance,
    decimal PreviousIncome,
    decimal PreviousExpense,
    IReadOnlyList<WeeklyCategoryDto> TopCategories,
    WeeklyUpcomingDto Upcoming,
    WeeklyOverdueDto Overdue,
    IReadOnlyList<WeeklyBudgetAlertDto> BudgetAlerts,
    bool HasActivity);

public record WeeklyCategoryDto(
    string Name,
    string Icon,
    decimal Amount,
    decimal PreviousAmount,
    decimal Percentage);

public record WeeklyUpcomingDto(
    int ExpenseCount,
    decimal ExpenseAmount,
    decimal IncomeAmount,
    IReadOnlyList<WeeklyUpcomingItemDto> Items);

public record WeeklyUpcomingItemDto(
    string Description,
    decimal Amount,
    DateTime Date,
    TransactionType Type);

public record WeeklyOverdueDto(int Count, decimal Amount);

public record WeeklyBudgetAlertDto(
    string CategoryName,
    decimal LimitAmount,
    decimal SpentAmount,
    decimal Percentage);

/// <summary>Usuário para quem o resumo da semana está na hora de sair (segunda-feira, a partir das 8h no fuso dele).</summary>
public record WeeklySummaryCandidateDto(
    Guid UserId,
    DateTime WeekStart,
    bool NotifyInApp,
    bool NotifyEmail);

/// <summary>Texto da notificação no app. <see cref="ReferenceId"/> é estável por usuário e semana.</summary>
public record WeeklySummaryNotificationDto(
    Guid UserId,
    DateTime WeekStart,
    Guid ReferenceId,
    string Message);
