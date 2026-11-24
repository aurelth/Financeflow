using MediatR;

namespace FinanceFlow.Application.UseCases.Recurring.Commands.ResetDismissedRecurring;

/// <summary>Esquece as sugestões dispensadas: elas voltam a ser oferecidas. Devolve quantas voltaram.</summary>
public record ResetDismissedRecurringCommand(Guid UserId) : IRequest<int>;
