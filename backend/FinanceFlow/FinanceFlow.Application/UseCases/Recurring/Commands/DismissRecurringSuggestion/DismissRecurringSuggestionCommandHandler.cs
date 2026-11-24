using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Recurring.Commands.DismissRecurringSuggestion;

public class DismissRecurringSuggestionCommandHandler(IDismissedRecurrenceRepository dismissedRepository)
    : IRequestHandler<DismissRecurringSuggestionCommand>
{
    public async Task Handle(DismissRecurringSuggestionCommand request, CancellationToken cancellationToken)
    {
        // Idempotente: dispensar duas vezes (duplo clique, duas abas) não é erro
        if (await dismissedRepository.ExistsAsync(request.UserId, request.Type, request.Key, cancellationToken))
            return;

        await dismissedRepository.AddAsync(
            DismissedRecurrence.Create(request.UserId, request.Type, request.Key),
            cancellationToken);
    }
}
