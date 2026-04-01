using FinanceFlow.Application.DTOs;
using MediatR;

namespace FinanceFlow.Application.UseCases.Reports.Queries.GetReports;

public record GetReportsQuery(
    Guid UserId
) : IRequest<IEnumerable<ReportDto>>;
