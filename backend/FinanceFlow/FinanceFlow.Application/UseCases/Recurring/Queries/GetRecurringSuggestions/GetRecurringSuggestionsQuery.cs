using FinanceFlow.Application.DTOs.Recurring;
using MediatR;

namespace FinanceFlow.Application.UseCases.Recurring.Queries.GetRecurringSuggestions;

/// <summary>Detecta assinaturas e contas recorrentes no histórico do usuário.</summary>
public record GetRecurringSuggestionsQuery(Guid UserId) : IRequest<RecurringSuggestionsDto>;
