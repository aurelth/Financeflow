using FinanceFlow.Application.DTOs.Reports;
using MediatR;

namespace FinanceFlow.Application.UseCases.Analytics.Queries.GetAnnualSummary;

public record GetAnnualSummaryQuery(
    Guid UserId,
    int Year
) : IRequest<AnnualSummaryDto>;
