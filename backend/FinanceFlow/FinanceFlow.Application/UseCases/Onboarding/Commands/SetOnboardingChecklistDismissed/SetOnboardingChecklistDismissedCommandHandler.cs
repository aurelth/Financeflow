using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Onboarding.Commands.SetOnboardingChecklistDismissed;

public class SetOnboardingChecklistDismissedCommandHandler(
    IUserRepository userRepository,
    TimeProvider timeProvider) : IRequestHandler<SetOnboardingChecklistDismissedCommand>
{
    public async Task Handle(
        SetOnboardingChecklistDismissedCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("Utilizador", request.UserId);

        // Idempotente: repetir o pedido não muda o estado nem a data original
        if (user.OnboardingChecklistDismissedAt.HasValue == request.Dismissed)
            return;

        user.OnboardingChecklistDismissedAt = request.Dismissed
            ? timeProvider.GetUtcNow().UtcDateTime
            : null;

        await userRepository.UpdateAsync(user, cancellationToken);
    }
}
