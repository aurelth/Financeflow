using FinanceFlow.Application.Services;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;

namespace FinanceFlow.Application.UseCases.Recurring;

/// <summary>Carrega o histórico e as dispensas do usuário e roda a detecção. Compartilhado por listar e confirmar,
/// para que a confirmação sempre parta do que o servidor detecta agora, nunca do que o cliente diz.</summary>
internal static class RecurringSuggestionLoader
{
    public static async Task<IReadOnlyList<DetectedRecurrence>> LoadAsync(
        Guid userId,
        ITransactionRepository transactionRepository,
        IDismissedRecurrenceRepository dismissedRepository,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        var today = clock.GetUtcNow().UtcDateTime.Date;
        var from = today.AddMonths(-RecurrenceDetector.LookbackMonths);

        var history = await transactionRepository.GetForRecurrenceAnalysisAsync(userId, from, cancellationToken);
        var dismissed = await dismissedRepository.GetByUserAsync(userId, cancellationToken);

        var dismissedKeys = dismissed
            .Select(d => (d.Type, d.Key))
            .ToHashSet();

        return RecurrenceDetector.Detect(history, dismissedKeys, today);
    }
}
