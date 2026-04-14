using FinanceFlow.Application.DTOs.Reports;
using MediatR;

namespace FinanceFlow.Application.UseCases.Analytics.Queries.GetProjections;

public record GetProjectionsQuery(
    Guid UserId,
    int MonthsBack,  // quantos meses históricos analisar (padrão: 12)
    int MonthsAhead  // quantos meses projectar (padrão: 3)
) : IRequest<ProjectionsDto>;
