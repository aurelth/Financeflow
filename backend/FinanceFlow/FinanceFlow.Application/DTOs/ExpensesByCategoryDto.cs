namespace FinanceFlow.Application.DTOs;

public record ExpensesByCategoryDto(
    string CategoryId,
    string CategoryName,
    string CategoryIcon,
    string CategoryColor,
    decimal Total,
    decimal Percentage
);
