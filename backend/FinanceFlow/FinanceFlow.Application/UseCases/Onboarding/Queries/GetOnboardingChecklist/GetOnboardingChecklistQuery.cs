using FinanceFlow.Application.DTOs.Onboarding;
using MediatR;

namespace FinanceFlow.Application.UseCases.Onboarding.Queries.GetOnboardingChecklist;

public record GetOnboardingChecklistQuery(Guid UserId) : IRequest<OnboardingChecklistDto>;
