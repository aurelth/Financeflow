using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.DTOs.Goals;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Goals.Commands.CreateGoal;

public class CreateGoalCommandHandler(
    IGoalRepository goalRepository,
    IGoalProgressService goalProgressService) : IRequestHandler<CreateGoalCommand, GoalProgressResultDto>
{
    public async Task<GoalProgressResultDto> Handle(
        CreateGoalCommand request,
        CancellationToken cancellationToken)
    {
        var goal = new Goal
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Name = request.Name,
            TargetAmount = request.TargetAmount,
            MonthlyContribution = request.MonthlyContribution,
            Deadline = request.Deadline,
            Emoji = request.Emoji,
            CreatedAt = DateTime.UtcNow,
        };

        await goalRepository.AddAsync(goal, cancellationToken);

        // Recalcula o progresso e retorna o resultado atualizado
        var summary = await goalProgressService.CalculateAsync(request.UserId, cancellationToken);
        var result = summary.Goals.FirstOrDefault(g => g.Id == goal.Id)
            ?? throw new NotFoundException(nameof(Goal), goal.Id);

        return result;
    }
}
