using FinanceFlow.Application.DTOs.Goals;
using MediatR;

namespace FinanceFlow.Application.UseCases.Goals.Queries.GetGoalsSummary;

public record GetGoalsSummaryQuery(
    Guid UserId
) : IRequest<GoalsSummaryResultDto>;
