namespace FinanceFlow.Application.DTOs;

public record UpdateReportStatusDto(
    string Status,
    string? FilePath,
    string? FileName
);
