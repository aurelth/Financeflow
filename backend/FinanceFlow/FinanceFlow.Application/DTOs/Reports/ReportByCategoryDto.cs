using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Application.DTOs.Reports;

public record ReportByCategoryDto(
    DateTime From,
    DateTime To,
    decimal TotalExpenses,
    decimal TotalIncome,
    IEnumerable<CategoryReportItemDto> Categories
);

public record CategoryReportItemDto(
    Guid CategoryId,
    string CategoryName,
    string CategoryIcon,
    string CategoryColor,
    TransactionType Type,
    decimal Amount,
    decimal Percentage,
    int TransactionCount,
    IEnumerable<SubcategoryReportItemDto> Subcategories
);

public record SubcategoryReportItemDto(
    Guid SubcategoryId,
    string SubcategoryName,
    decimal Amount,
    decimal Percentage,
    int TransactionCount
);
