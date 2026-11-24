using FinanceFlow.Domain.Entities;
using MediatR;

namespace FinanceFlow.Application.UseCases.Recurring.Commands.DismissRecurringSuggestion;

/// <summary>"Não é uma assinatura": a sugestão deixa de aparecer para esta descrição.</summary>
public record DismissRecurringSuggestionCommand(Guid UserId, TransactionType Type, string Key) : IRequest;
