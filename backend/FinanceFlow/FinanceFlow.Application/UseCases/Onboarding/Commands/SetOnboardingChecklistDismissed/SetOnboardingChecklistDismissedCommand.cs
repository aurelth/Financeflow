using MediatR;

namespace FinanceFlow.Application.UseCases.Onboarding.Commands.SetOnboardingChecklistDismissed;

/// <summary>Dispensa (ou volta a mostrar) o checklist de primeiros passos.</summary>
public record SetOnboardingChecklistDismissedCommand(Guid UserId, bool Dismissed) : IRequest;
