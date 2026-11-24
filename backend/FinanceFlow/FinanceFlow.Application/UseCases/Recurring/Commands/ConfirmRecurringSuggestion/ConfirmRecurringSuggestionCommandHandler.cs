using System.Text.Json;
using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.DTOs.Recurring;
using FinanceFlow.Application.Services;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Recurring.Commands.ConfirmRecurringSuggestion;

public class ConfirmRecurringSuggestionCommandHandler(
    ITransactionRepository transactionRepository,
    IDismissedRecurrenceRepository dismissedRepository,
    IDashboardCacheInvalidator dashboardCacheInvalidator,
    TimeProvider clock)
    : IRequestHandler<ConfirmRecurringSuggestionCommand, ConfirmRecurringResultDto>
{
    public async Task<ConfirmRecurringResultDto> Handle(
        ConfirmRecurringSuggestionCommand request,
        CancellationToken cancellationToken)
    {
        var today = clock.GetUtcNow().UtcDateTime.Date;

        // Parte do que o servidor detecta agora: o cliente só aponta qual sugestão confirmar
        var detected = await RecurringSuggestionLoader.LoadAsync(
            request.UserId, transactionRepository, dismissedRepository, clock, cancellationToken);

        var recurrence = detected.FirstOrDefault(d => d.Type == request.Type && d.Key == request.Key)
            ?? throw new NotFoundException("RecurringSuggestion", request.Key);

        var groupId = Guid.NewGuid();

        // O histórico passa a pertencer ao grupo: editar/remover "esta e as futuras" funciona como em
        // qualquer recorrência, e a lista mostra o selo de recorrência em toda a série.
        foreach (var occurrence in recurrence.Occurrences)
        {
            occurrence.IsRecurring = true;
            occurrence.RecurrenceType = recurrence.Cadence;
            occurrence.RecurrenceGroupId = groupId;
        }

        var latest = recurrence.Latest;
        var projected = RecurrenceDetector.ProjectDates(recurrence, today)
            .Select(date => new Transaction
            {
                UserId = latest.UserId,
                Amount = recurrence.Amount,
                Type = latest.Type,
                Date = date,
                Description = latest.Description,
                Status = TransactionStatus.Scheduled,
                IsRecurring = true,
                RecurrenceType = recurrence.Cadence,
                RecurrenceGroupId = groupId,
                AccountId = latest.AccountId,
                CategoryId = latest.CategoryId,
                SubcategoryId = latest.SubcategoryId,
                Tags = string.IsNullOrWhiteSpace(latest.Tags) ? JsonSerializer.Serialize(Array.Empty<string>()) : latest.Tags,
            })
            .ToList();

        await transactionRepository.SaveRecurringSeriesAsync(recurrence.Occurrences, projected, cancellationToken);

        await dashboardCacheInvalidator.InvalidateAsync(
            request.UserId, projected.Select(p => p.Date), cancellationToken);

        return new ConfirmRecurringResultDto(
            RecurrenceGroupId: groupId,
            Cadence: recurrence.Cadence,
            LinkedCount: recurrence.Occurrences.Count,
            ProjectedCount: projected.Count,
            NextProjectedDate: projected.Count > 0 ? projected[0].Date : null);
    }
}
