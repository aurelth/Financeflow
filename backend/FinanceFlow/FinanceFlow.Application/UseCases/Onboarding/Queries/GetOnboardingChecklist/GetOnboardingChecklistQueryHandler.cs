using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.DTOs.Onboarding;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Onboarding.Queries.GetOnboardingChecklist;

public class GetOnboardingChecklistQueryHandler(
    IOnboardingRepository onboardingRepository,
    IUserRepository userRepository) : IRequestHandler<GetOnboardingChecklistQuery, OnboardingChecklistDto>
{
    public async Task<OnboardingChecklistDto> Handle(
        GetOnboardingChecklistQuery request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("Utilizador", request.UserId);

        var progress = await onboardingRepository.GetProgressAsync(request.UserId, cancellationToken);

        // A ordem é a sugerida ao usuário
        IReadOnlyList<OnboardingStepDto> steps =
        [
            new(OnboardingSteps.Transaction, progress.HasTransaction),
            new(OnboardingSteps.Import, progress.HasConfirmedImport),
            new(OnboardingSteps.Budget, progress.HasBudget),
            new(OnboardingSteps.Goal, progress.HasGoal),
        ];

        return new OnboardingChecklistDto(
            steps,
            Completed: steps.Count(s => s.Done),
            Total: steps.Count,
            Dismissed: user.OnboardingChecklistDismissedAt.HasValue);
    }
}
