using MediatR;

namespace FinanceFlow.Application.UseCases.Reports.Queries.GetReportDownload;

public record GetReportDownloadQuery(
    Guid ReportId,
    Guid UserId
) : IRequest<ReportDownloadResult>;

public record ReportDownloadResult(
    string FilePath,
    string FileName,
    string ContentType
);
