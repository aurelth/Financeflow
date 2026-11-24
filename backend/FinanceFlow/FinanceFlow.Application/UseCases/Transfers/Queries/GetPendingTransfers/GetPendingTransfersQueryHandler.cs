using FinanceFlow.Application.DTOs.Transfers;
using FinanceFlow.Application.Services;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Transfers.Queries.GetPendingTransfers;

/// <summary>
/// Pontas de transferência que ainda não têm par, cada uma com a melhor sugestão de par encontrada
/// entre os lançamentos sem vínculo das outras contas. Reavalia a cada consulta: se o extrato da outra
/// conta foi importado depois, a sugestão aparece sem ninguém precisar refazer nada.
/// </summary>
public class GetPendingTransfersQueryHandler(ITransferRepository transferRepository)
    : IRequestHandler<GetPendingTransfersQuery, IReadOnlyList<PendingTransferDto>>
{
    private const int MaxPending = 100;

    public async Task<IReadOnlyList<PendingTransferDto>> Handle(
        GetPendingTransfersQuery request,
        CancellationToken cancellationToken)
    {
        var pending = await transferRepository.GetUnlinkedTransfersAsync(
            request.UserId, MaxPending, cancellationToken);

        if (pending.Count == 0)
            return [];

        var from = pending.Min(t => t.Date).Date.AddDays(-TransferMatchingService.MaxDaysApart);
        var to = pending.Max(t => t.Date).Date.AddDays(TransferMatchingService.MaxDaysApart + 1).AddTicks(-1);

        var candidates = await transferRepository.GetCandidatesAsync(
            request.UserId, excludeAccountId: null, from, to, cancellationToken);

        var rejected = await transferRepository.GetRejectionsAsync(
            request.UserId,
            pending.Select(t => t.Id).ToList(),
            cancellationToken);

        var rejectedPairs = rejected
            .Select(r => (r.FirstTransactionId, r.SecondTransactionId))
            .ToHashSet();

        var result = new List<PendingTransferDto>(pending.Count);

        foreach (var item in pending)
        {
            var suggestion = candidates
                .Where(c => c.Id != item.Id && c.AccountId != item.AccountId)
                .Where(c => !rejectedPairs.Contains(Ordered(item.Id, c.Id)))
                .Select(c => (Candidate: c, Score: ScoreOf(item, c)))
                .Where(x => x.Score >= TransferMatchingService.SuggestedThreshold)
                .OrderByDescending(x => x.Score)
                .ThenBy(x => Math.Abs((x.Candidate.Date.Date - item.Date.Date).TotalDays))
                .Cast<(Transaction Candidate, int Score)?>()
                .FirstOrDefault();

            result.Add(new PendingTransferDto(
                Transaction: TransferSideDto.From(item),
                Suggestion: suggestion is { } s
                    ? new TransferSuggestionDto(
                        TransferSideDto.From(s.Candidate),
                        s.Score,
                        WillConvert: s.Candidate.Type != TransactionType.Transfer)
                    : null));
        }

        // Quem tem sugestão primeiro: é onde o usuário resolve com um clique
        return result
            .OrderByDescending(r => r.Suggestion is not null)
            .ThenByDescending(r => r.Transaction.Date)
            .ToList();
    }

    private static int ScoreOf(Transaction pending, Transaction candidate)
    {
        var pendingDirection = TransferMatchingService.DirectionOf(pending.Type, pending.TransferDirection);
        var candidateDirection = TransferMatchingService.DirectionOf(candidate.Type, candidate.TransferDirection);

        if (pendingDirection is null || candidateDirection is null || pendingDirection == candidateDirection)
            return 0;

        return TransferMatchingService.Score(
            pending.Amount, pending.Date, pending.Description,
            candidate.Amount, candidate.Date, candidate.Description,
            candidate.Type == TransactionType.Transfer);
    }

    private static (Guid, Guid) Ordered(Guid a, Guid b) => a.CompareTo(b) <= 0 ? (a, b) : (b, a);
}
