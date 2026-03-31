namespace FinanceFlow.Application.DTOs;

public record CategoryComparisonDto(
    string CategoryId,
    string CategoryName,
    string CategoryIcon,
    string CategoryColor,
    decimal[] Values,
    decimal?[] Variations
);
