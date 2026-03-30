namespace FinanceFlow.Application.DTOs;

public record BudgetDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string CategoryIcon,
    string CategoryColor,
    int Month,
    int Year,
    decimal LimitAmount,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
