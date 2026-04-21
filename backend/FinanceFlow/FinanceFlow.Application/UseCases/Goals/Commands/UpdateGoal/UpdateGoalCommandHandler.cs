using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.DTOs.Goals;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Goals.Commands.UpdateGoal;

public class UpdateGoalCommandHandler(
    IGoalRepository goalRepository,
    IGoalProgressService goalProgressService) : IRequestHandler<UpdateGoalCommand, GoalProgressResultDto>
{
    public async Task<GoalProgressResultDto> Handle(
        UpdateGoalCommand request,
        CancellationToken cancellationToken)
    {
        var goal = await goalRepository.GetByIdAsync(request.Id, request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Goal), request.Id);

        // Modificado — Goal é class, não record, atualiza propriedades diretamente
        goal.Name = request.Name;
        goal.TargetAmount = request.TargetAmount;
        goal.MonthlyContribution = request.MonthlyContribution;
        goal.Deadline = request.Deadline;
        goal.Emoji = request.Emoji;

        await goalRepository.UpdateAsync(goal, cancellationToken);

        var summary = await goalProgressService.CalculateAsync(request.UserId, cancellationToken);
        var result = summary.Goals.FirstOrDefault(g => g.Id == request.Id)
            ?? throw new NotFoundException(nameof(Goal), request.Id);

        return result;
    }
}
