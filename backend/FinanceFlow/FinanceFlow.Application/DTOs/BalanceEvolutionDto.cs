namespace FinanceFlow.Application.DTOs;

public record BalanceEvolutionDto(
    string Date,
    decimal Income,
    decimal Expenses,
    decimal Balance
);
