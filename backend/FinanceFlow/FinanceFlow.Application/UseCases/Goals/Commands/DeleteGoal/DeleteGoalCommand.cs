using MediatR;

namespace FinanceFlow.Application.UseCases.Goals.Commands.DeleteGoal;

public record DeleteGoalCommand(
    Guid Id,
    Guid UserId
) : IRequest;
