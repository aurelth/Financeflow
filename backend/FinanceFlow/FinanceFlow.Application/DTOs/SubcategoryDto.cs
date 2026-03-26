namespace FinanceFlow.Application.DTOs;

public record SubcategoryDto(
    Guid Id,
    string Name,
    bool IsActive
);
