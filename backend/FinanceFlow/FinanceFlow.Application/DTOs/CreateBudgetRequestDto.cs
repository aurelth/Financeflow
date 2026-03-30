namespace FinanceFlow.Application.DTOs;

public record CreateBudgetRequestDto(
    Guid CategoryId,
    int Month,
    int Year,
    decimal LimitAmount
);
