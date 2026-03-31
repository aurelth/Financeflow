using FinanceFlow.Domain.Enums;

namespace FinanceFlow.Application.DTOs;

public record ReportDto(
    Guid Id,
    ReportType Type,
    ReportStatus Status,
    int Month,
    int Year,
    string? FileName,
    DateTime CreatedAt,
    DateTime? CompletedAt
);
