using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Interfaces;

namespace FinanceFlow.Application.Services;

public interface IImportDeduplicationService
{
    /// <summary>
    /// Marca no staging as linhas que já existem na conta da importação.
    /// É idempotente: zera as marcações anteriores antes de recalcular.
    /// Retorna a quantidade de linhas marcadas como duplicadas.
    /// </summary>
    Task<int> DetectAsync(BankImport bankImport, CancellationToken cancellationToken = default);
}

public class ImportDeduplicationService(IBankImportRepository bankImportRepository)
    : IImportDeduplicationService
{
    /// <summary>Tolerância (em dias) para casar uma linha do extrato com um lançamento manual.</summary>
    public const int ManualMatchToleranceDays = 2;

    public async Task<int> DetectAsync(BankImport bankImport, CancellationToken cancellationToken = default)
    {
        var rows = bankImport.Transactions.ToList();

        foreach (var row in rows)
        {
            row.IsDuplicate = false;
            row.IsSelected = true;
            row.DuplicateReason = null;
            row.MatchedTransactionId = null;
        }

        if (rows.Count == 0)
            return 0;

        var hashes = rows.Select(r => r.Hash).Distinct().ToList();
        var externalIds = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.ExternalId))
            .Select(r => r.ExternalId)
            .Distinct()
            .ToList();

        var keys = await bankImportRepository.GetImportedKeysAsync(
            bankImport.UserId, bankImport.AccountId, hashes, externalIds, cancellationToken);

        var byHash = keys.Where(k => k.ImportHash is not null).ToLookup(k => k.ImportHash!);
        var byFitId = keys.Where(k => k.ExternalId is not null).ToLookup(k => k.ExternalId!);

        foreach (var row in rows)
        {
            // 1) FITID é o identificador do banco: o mais confiável (confere o valor para
            //    proteger contra bancos que reaproveitam o mesmo FITID)
            if (!string.IsNullOrWhiteSpace(row.ExternalId))
            {
                var byId = byFitId[row.ExternalId].FirstOrDefault(k => k.Amount == row.Amount);
                if (byId is not null)
                {
                    MarkDuplicate(row, DuplicateReason.SameFitId, byId.TransactionId);
                    continue;
                }
            }

            // 2) Sem FITID aproveitável: data + valor + descrição + ocorrência
            var sameContent = byHash[row.Hash].FirstOrDefault();
            if (sameContent is not null)
                MarkDuplicate(row, DuplicateReason.SameContent, sameContent.TransactionId);
        }

        await MarkPossibleManualMatchesAsync(bankImport, rows, cancellationToken);

        return rows.Count(r => r.IsDuplicate);
    }

    // 3) Lançamentos digitados à mão: mesmo valor e tipo, data até ±2 dias
    private async Task MarkPossibleManualMatchesAsync(
        BankImport bankImport,
        List<BankImportTransaction> rows,
        CancellationToken cancellationToken)
    {
        var pending = rows.Where(r => !r.IsDuplicate).ToList();
        if (pending.Count == 0)
            return;

        var from = pending.Min(r => r.Date).Date.AddDays(-ManualMatchToleranceDays);
        var to = pending.Max(r => r.Date).Date.AddDays(ManualMatchToleranceDays + 1).AddTicks(-1);

        var candidates = await bankImportRepository.GetManualCandidatesAsync(
            bankImport.UserId, bankImport.AccountId, from, to, cancellationToken);

        if (candidates.Count == 0)
            return;

        // Cada lançamento manual só pode "explicar" uma linha do extrato
        var used = new HashSet<Guid>();

        foreach (var row in pending.OrderBy(r => r.Date))
        {
            var match = candidates
                .Where(c =>
                    !used.Contains(c.Id) &&
                    c.Type == row.Type &&
                    c.Amount == row.Amount &&
                    Math.Abs((c.Date.Date - row.Date.Date).TotalDays) <= ManualMatchToleranceDays)
                .OrderBy(c => Math.Abs((c.Date.Date - row.Date.Date).TotalDays))
                .FirstOrDefault();

            if (match is null)
                continue;

            used.Add(match.Id);
            MarkDuplicate(row, DuplicateReason.PossibleManual, match.Id);
        }
    }

    private static void MarkDuplicate(BankImportTransaction row, DuplicateReason reason, Guid matchedId)
    {
        row.IsDuplicate = true;
        row.IsSelected = false;
        row.DuplicateReason = reason;
        row.MatchedTransactionId = matchedId;
    }
}
