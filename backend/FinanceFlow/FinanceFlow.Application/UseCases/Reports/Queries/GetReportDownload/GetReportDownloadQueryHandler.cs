using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Reports.Queries.GetReportDownload;

public class GetReportDownloadQueryHandler(
    IReportRepository reportRepository)
    : IRequestHandler<GetReportDownloadQuery, ReportDownloadResult>
{
    public async Task<ReportDownloadResult> Handle(
        GetReportDownloadQuery request,
        CancellationToken cancellationToken)
    {
        var report = await reportRepository.GetByIdAsync(
            request.ReportId, request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Report), request.ReportId);

        if (report.Status != ReportStatus.Completed || report.FilePath is null)
            throw new ValidationException("O relatório ainda não está disponível para download.");

        return new ReportDownloadResult(
            FilePath: report.FilePath,
            FileName: report.FileName ?? $"report_{report.Month}_{report.Year}.csv",
            ContentType: "text/csv");
    }
}
