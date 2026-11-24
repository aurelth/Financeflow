using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Recurring.Commands.ResetDismissedRecurring;

public class ResetDismissedRecurringCommandHandler(IDismissedRecurrenceRepository dismissedRepository)
    : IRequestHandler<ResetDismissedRecurringCommand, int>
{
    public Task<int> Handle(ResetDismissedRecurringCommand request, CancellationToken cancellationToken) =>
        dismissedRepository.RemoveAllAsync(request.UserId, cancellationToken);
}
