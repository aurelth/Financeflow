using MediatR;

namespace FinanceFlow.Application.UseCases.Reports.Commands.UpdateReportStatus;

public record UpdateReportStatusCommand(
    Guid ReportId,
    string Status,
    string? FilePath,
    string? FileName
) : IRequest;
