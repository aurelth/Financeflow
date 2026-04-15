using FinanceFlow.Application.DTOs.Reports;
using MediatR;

namespace FinanceFlow.Application.UseCases.Analytics.Queries.GetReportByTag;

public record GetReportByTagQuery(
    Guid UserId,
    DateTime From,
    DateTime To
) : IRequest<ReportByTagDto>;
