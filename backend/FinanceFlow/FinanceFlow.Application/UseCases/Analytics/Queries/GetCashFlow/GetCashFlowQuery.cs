using FinanceFlow.Application.DTOs.Reports;
using MediatR;

namespace FinanceFlow.Application.UseCases.Analytics.Queries.GetCashFlow;

public record GetCashFlowQuery(
    Guid UserId,
    DateTime From,
    DateTime To,
    string GroupBy // "day" ou "month"
) : IRequest<CashFlowDto>;
