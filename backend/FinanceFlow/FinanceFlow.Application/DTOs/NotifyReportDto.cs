namespace FinanceFlow.Application.DTOs;

public record NotifyReportDto(
    Guid UserId,
    Guid ReportId,
    string? FileName
);
