using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.DTOs.Imports;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Imports.Queries.GetImportPreview;

public class GetImportPreviewQueryHandler(
    IBankImportRepository bankImportRepository,
    ITransferRepository transferRepository)
    : IRequestHandler<GetImportPreviewQuery, BankImportPreviewDto>
{
    public async Task<BankImportPreviewDto> Handle(
        GetImportPreviewQuery request,
        CancellationToken cancellationToken)
    {
        var bankImport = await bankImportRepository.GetByIdAsync(
            request.ImportId, request.UserId, cancellationToken)
            ?? throw new NotFoundException("BankImport", request.ImportId);

        // Lançamentos existentes que originaram as suspeitas de duplicata, para o usuário comparar
        var matchedIds = bankImport.Transactions
            .Where(t => t.MatchedTransactionId.HasValue)
            .Select(t => t.MatchedTransactionId!.Value)
            .Distinct()
            .ToList();

        var matched = (await bankImportRepository.GetTransactionsByIdsAsync(
                request.UserId, matchedIds, cancellationToken))
            .ToDictionary(t => t.Id);

        // Pares de transferência: só aparecem enquanto a outra ponta continua existindo e sem vínculo
        var transferMatches = await LoadTransferMatchesAsync(bankImport, request.UserId, cancellationToken);

        var transactions = bankImport.Transactions
            .OrderBy(t => t.Date.Date)
            .ThenBy(t => t.Date)
            .Select(t => new BankImportTransactionDto(
                Id: t.Id,
                ExternalId: t.ExternalId,
                Date: t.Date,
                Amount: t.Amount,
                Description: t.Description,
                Type: t.Type,
                IsDuplicate: t.IsDuplicate,
                IsSelected: t.IsSelected,
                SuggestedCategoryId: t.SuggestedCategoryId,
                SuggestedCategoryName: t.SuggestedCategory?.Name,
                DuplicateReason: t.DuplicateReason,
                Match: t.MatchedTransactionId is { } id && matched.TryGetValue(id, out var m)
                    ? new DuplicateMatchDto(m.Id, m.Date, m.Amount, m.Description)
                    : null,
                TransferMatch: transferMatches.GetValueOrDefault(t.Id),
                SuggestionSource: t.SuggestedCategoryId.HasValue ? t.SuggestionSource : null))
            .ToList();

        return new BankImportPreviewDto(
            ImportId: bankImport.Id,
            FileName: bankImport.FileName,
            TotalRecords: bankImport.TotalRecords,
            Duplicates: bankImport.Duplicates,
            Transactions: transactions,
            AccountId: bankImport.AccountId,
            AccountName: bankImport.Account?.Name,
            Status: bankImport.Status.ToString(),
            AutoTransfers: transferMatches.Values.Count(m => m.Status == nameof(TransferMatchStatus.Auto)),
            SuggestedTransfers: transferMatches.Values.Count(m => m.Status == nameof(TransferMatchStatus.Suggested)));
    }

    private async Task<Dictionary<Guid, TransferMatchDto>> LoadTransferMatchesAsync(
        BankImport bankImport,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var rows = bankImport.Transactions
            .Where(t => t.TransferMatchTransactionId.HasValue && t.TransferMatchStatus.HasValue)
            .ToList();

        if (rows.Count == 0)
            return [];

        var ids = rows.Select(r => r.TransferMatchTransactionId!.Value).Distinct().ToList();
        var candidates = (await bankImportRepository.GetTransactionsByIdsAsync(userId, ids, cancellationToken))
            .ToDictionary(c => c.Id);

        var result = new Dictionary<Guid, TransferMatchDto>();

        foreach (var row in rows)
        {
            if (!candidates.TryGetValue(row.TransferMatchTransactionId!.Value, out var candidate))
                continue;

            if (candidate.DeletedAt is not null)
                continue;

            if (await transferRepository.IsLinkedAsync(candidate.Id, cancellationToken))
                continue;

            result[row.Id] = new TransferMatchDto(
                TransactionId: candidate.Id,
                AccountId: candidate.AccountId,
                AccountName: candidate.Account?.Name,
                Date: candidate.Date,
                Amount: candidate.Amount,
                Description: candidate.Description,
                Score: row.TransferMatchScore ?? 0,
                Status: row.TransferMatchStatus!.Value.ToString(),
                WillConvert: candidate.Type != TransactionType.Transfer);
        }

        return result;
    }
}
