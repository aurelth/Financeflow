using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Application.DTOs.Calendar;

/// <summary>Um lançamento no calendário: conta a pagar (despesa) ou a receber (receita).</summary>
public record CalendarEntryDto(
    Guid Id,
    string Description,
    decimal Amount,
    TransactionType Type,
    TransactionStatus Status,
    DateTime Date,
    bool IsRecurring,
    int? InstallmentNumber,
    int? InstallmentCount,
    Guid CategoryId,
    string CategoryName,
    string CategoryIcon,
    string CategoryColor,
    Guid AccountId,
    string AccountName,
    /// <summary>Em aberto e com data anterior a hoje (no fuso do usuário).</summary>
    bool IsOverdue);

/// <summary>Totais de um dia (ou do mês): "em aberto" são Pendentes/Agendados; "liquidado" são os Pagos.</summary>
public record CalendarTotalsDto(
    decimal ToPay,
    decimal ToReceive,
    decimal Paid,
    decimal Received);

public record CalendarDayDto(
    DateTime Date,
    CalendarTotalsDto Totals,
    IReadOnlyList<CalendarEntryDto> Entries);

public record CalendarOverdueDto(
    int Count,
    decimal ExpenseAmount,
    decimal IncomeAmount,
    IReadOnlyList<CalendarEntryDto> Entries);

public record CalendarMonthDto(
    int Month,
    int Year,
    /// <summary>Hoje no fuso do usuário (yyyy-MM-dd): é a referência de "vencida".</summary>
    DateTime Today,
    /// <summary>Só os dias que têm lançamentos, em ordem.</summary>
    IReadOnlyList<CalendarDayDto> Days,
    CalendarTotalsDto Totals,
    /// <summary>Vencidas e ainda não pagas (últimos 90 dias), de qualquer mês.</summary>
    CalendarOverdueDto Overdue);
