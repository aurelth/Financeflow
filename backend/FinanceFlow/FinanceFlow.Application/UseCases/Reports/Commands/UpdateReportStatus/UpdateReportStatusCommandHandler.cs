using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Reports.Commands.UpdateReportStatus;

public class UpdateReportStatusCommandHandler(
    IReportRepository reportRepository)
    : IRequestHandler<UpdateReportStatusCommand>
{
    public async Task Handle(
        UpdateReportStatusCommand request,
        CancellationToken cancellationToken)
    {
        var report = await reportRepository.GetByIdInternalAsync(
            request.ReportId, cancellationToken)
            ?? throw new NotFoundException(nameof(Report), request.ReportId);

        report.Status = request.Status switch
        {
            "Processing" => ReportStatus.Processing,
            "Completed" => ReportStatus.Completed,
            "Failed" => ReportStatus.Failed,
            _ => report.Status,
        };

        if (request.FilePath is not null) report.FilePath = request.FilePath;
        if (request.FileName is not null) report.FileName = request.FileName;
        if (report.Status == ReportStatus.Completed) report.CompletedAt = DateTime.UtcNow;

        await reportRepository.UpdateAsync(report, cancellationToken);
    }
}
