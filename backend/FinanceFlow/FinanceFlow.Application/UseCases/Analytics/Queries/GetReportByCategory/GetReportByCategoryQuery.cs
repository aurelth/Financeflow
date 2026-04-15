using FinanceFlow.Application.DTOs.Reports;
using FinanceFlow.Domain.Entities;
using MediatR;

namespace FinanceFlow.Application.UseCases.Analytics.Queries.GetReportByCategory;

public record GetReportByCategoryQuery(
    Guid UserId,
    DateTime From,
    DateTime To,
    TransactionType? Type
) : IRequest<ReportByCategoryDto>;
