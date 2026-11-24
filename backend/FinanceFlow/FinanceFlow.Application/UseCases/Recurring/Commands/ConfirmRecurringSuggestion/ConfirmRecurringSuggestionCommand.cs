using FinanceFlow.Application.DTOs.Recurring;
using FinanceFlow.Domain.Entities;
using MediatR;

namespace FinanceFlow.Application.UseCases.Recurring.Commands.ConfirmRecurringSuggestion;

/// <summary>Confirma uma recorrência detectada: liga o histórico num grupo e projeta as próximas ocorrências.</summary>
public record ConfirmRecurringSuggestionCommand(Guid UserId, TransactionType Type, string Key)
    : IRequest<ConfirmRecurringResultDto>;
