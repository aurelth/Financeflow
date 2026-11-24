using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;

namespace FinanceFlow.Application.DTOs.Recurring;

/// <summary>Uma recorrência detectada no histórico, ainda não confirmada.</summary>
public record RecurringSuggestionDto(
    string Key,
    TransactionType Type,
    string Description,
    RecurrenceType Cadence,
    string Kind,
    decimal Amount,
    bool AmountVaries,
    int Occurrences,
    DateTime FirstDate,
    DateTime LastDate,
    DateTime NextExpectedDate,
    decimal MonthlyCost,
    double Confidence,
    Guid CategoryId,
    string CategoryName,
    string CategoryIcon,
    string CategoryColor,
    Guid AccountId,
    string AccountName);

/// <summary>Sugestões ordenadas da mais para a menos provável, com o custo mensal somado por natureza.</summary>
public record RecurringSuggestionsDto(
    IReadOnlyList<RecurringSuggestionDto> Items,
    decimal MonthlyExpenseTotal,
    decimal MonthlyIncomeTotal);

public record ConfirmRecurringResultDto(
    Guid RecurrenceGroupId,
    RecurrenceType Cadence,
    int LinkedCount,
    int ProjectedCount,
    DateTime? NextProjectedDate);
