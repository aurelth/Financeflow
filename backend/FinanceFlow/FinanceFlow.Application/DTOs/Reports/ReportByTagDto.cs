namespace FinanceFlow.Application.DTOs.Reports;

public record ReportByTagDto(
    DateTime From,
    DateTime To,
    decimal TotalAmount,
    IEnumerable<TagReportItemDto> Tags
);

public record TagReportItemDto(
    string Tag,
    decimal Amount,
    decimal Percentage,
    int TransactionCount
);
