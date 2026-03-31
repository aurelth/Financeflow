using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Reports.Queries.GetReports;

public class GetReportsQueryHandler(
    IReportRepository reportRepository)
    : IRequestHandler<GetReportsQuery, IEnumerable<ReportDto>>
{
    public async Task<IEnumerable<ReportDto>> Handle(
        GetReportsQuery request,
        CancellationToken cancellationToken)
    {
        var reports = await reportRepository.GetByUserAsync(
            request.UserId, cancellationToken);

        return reports.Select(r => new ReportDto(
            Id: r.Id,
            Type: r.Type,
            Status: r.Status,
            Month: r.Month,
            Year: r.Year,
            FileName: r.FileName,
            CreatedAt: r.CreatedAt,
            CompletedAt: r.CompletedAt));
    }
}
