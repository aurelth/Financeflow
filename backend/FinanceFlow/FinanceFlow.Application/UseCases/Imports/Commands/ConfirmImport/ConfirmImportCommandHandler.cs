using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.DTOs.Imports;
using FinanceFlow.Application.Services;
using FinanceFlow.Domain.Constants;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Imports.Commands.ConfirmImport;

public class ConfirmImportCommandHandler(
    IBankImportRepository bankImportRepository,
    ITransactionRepository transactionRepository,
    ICategoryRepository categoryRepository,
    ITransferRepository transferRepository,
    IDashboardCacheInvalidator dashboardCacheInvalidator)
    : IRequestHandler<ConfirmImportCommand, BankImportDto>
{
    /// <summary>Uma linha aprovada, já validada, pronta para virar lançamento.</summary>
    private sealed record PlannedRow(
        BankImportTransaction Row,
        TransactionType Type,
        Guid CategoryId,
        TransferDirection? Direction,
        Transaction? LinkWith,
        Guid? RejectedCandidateId);

    public async Task<BankImportDto> Handle(
        ConfirmImportCommand request,
        CancellationToken cancellationToken)
    {
        var bankImport = await bankImportRepository.GetByIdAsync(
            request.ImportId, request.UserId, cancellationToken)
            ?? throw new NotFoundException("BankImport", request.ImportId);

        if (bankImport.Imported > 0)
            throw new ValidationException("Esta importação já foi confirmada.");

        // Sem o processamento as duplicatas ainda não foram detectadas
        if (bankImport.Status is BankImportStatus.Pending or BankImportStatus.Processing)
            throw new ValidationException("A importação ainda está sendo processada. Tente novamente em instantes.");

        var selections = request.Request.Transactions
            .ToDictionary(t => t.Id);

        // Outras pontas das transferências: carregadas rastreadas, pois podem ser convertidas em Transferência
        var candidates = await LoadCandidatesAsync(bankImport, selections, request.UserId, cancellationToken);
        var usedCandidates = new HashSet<Guid>();

        // 1) Valida tudo antes de gravar qualquer coisa: linha sem categoria nunca some em silêncio
        var plan = new List<PlannedRow>();
        var rowErrors = new Dictionary<string, string[]>();
        var categoryIsValid = new Dictionary<Guid, bool>();

        foreach (var importTransaction in bankImport.Transactions)
        {
            if (!selections.TryGetValue(importTransaction.Id, out var selection))
                continue;

            if (!selection.IsSelected)
                continue;

            // Duplicada só entra se o usuário pediu explicitamente "importar mesmo assim"
            if (importTransaction.IsDuplicate && !selection.ImportAnyway)
                continue;

            // 1a) Par de transferência: vincula as duas pontas (a categoria é sempre "Transferência")
            var linkDecision = await DecideTransferLinkAsync(
                importTransaction, selection, candidates, usedCandidates, bankImport.AccountId, cancellationToken);

            if (linkDecision.Candidate is not null)
            {
                usedCandidates.Add(linkDecision.Candidate.Id);
                plan.Add(new PlannedRow(
                    importTransaction,
                    TransactionType.Transfer,
                    WellKnownIds.TransferCategoryId,
                    linkDecision.Direction,
                    linkDecision.Candidate,
                    null));
                continue;
            }

            // 1b) Lançamento comum (ou transferência sem par)
            var transactionType = linkDecision.ForceUnpairedTransfer
                ? TransactionType.Transfer
                : ResolveType(selection.Type, importTransaction.Type);
            var categoryId = selection.CategoryId;

            if (categoryId == Guid.Empty)
            {
                // Transferência não exige categoria: usa a categoria padrão
                if (transactionType != TransactionType.Transfer)
                {
                    rowErrors[importTransaction.Id.ToString()] = [ImportRowErrors.MissingCategory];
                    continue;
                }

                categoryId = WellKnownIds.TransferCategoryId;
            }

            if (categoryId != WellKnownIds.TransferCategoryId)
            {
                if (!categoryIsValid.TryGetValue(categoryId, out var valid))
                {
                    valid = await categoryRepository.GetByIdAsync(
                        categoryId, request.UserId, cancellationToken) is not null;
                    categoryIsValid[categoryId] = valid;
                }

                if (!valid)
                {
                    rowErrors[importTransaction.Id.ToString()] = [ImportRowErrors.InvalidCategory];
                    continue;
                }
            }

            // O sentido da transferência vem do extrato: crédito entra, débito sai
            TransferDirection? direction = null;
            if (transactionType == TransactionType.Transfer)
            {
                direction = TransferMatchingService.DirectionOf(importTransaction.Type, null);

                if (direction is null)
                {
                    rowErrors[importTransaction.Id.ToString()] = [ImportRowErrors.UnknownTransferDirection];
                    continue;
                }
            }

            // "Não é transferência" dito explicitamente: lembra o par para não sugerir de novo
            var rejected = linkDecision.RejectedCandidateId;

            plan.Add(new PlannedRow(importTransaction, transactionType, categoryId, direction, null, rejected));
        }

        if (rowErrors.Count > 0)
            throw new ValidationException(
                $"{rowErrors.Count} lançamento(s) sem categoria válida. Escolha a categoria ou desmarque a linha para confirmar.",
                rowErrors);

        // 2) Grava
        var imported = 0;
        var errors = 0;
        var touchedDates = new List<DateTime>();

        foreach (var item in plan)
        {
            try
            {
                var importTransaction = item.Row;
                importTransaction.SuggestedCategoryId = item.CategoryId;

                var transaction = new Transaction
                {
                    UserId = request.UserId,
                    AccountId = bankImport.AccountId,
                    Amount = importTransaction.Amount,
                    Type = item.Type,
                    TransferDirection = item.Direction,
                    Date = importTransaction.Date,
                    Description = importTransaction.Description,
                    Status = TransactionStatus.Paid,
                    CategoryId = item.CategoryId,
                    ImportHash = importTransaction.Hash,
                    ExternalId = string.IsNullOrWhiteSpace(importTransaction.ExternalId)
                        ? null
                        : importTransaction.ExternalId,
                    Tags = "[]",
                };

                if (item.LinkWith is { } other)
                {
                    // Lançamento novo + conversão da outra ponta + vínculo: tudo num único SaveChanges
                    LinkPair(request.UserId, transaction, other, importTransaction);
                    await transferRepository.SaveChangesAsync(cancellationToken);
                    touchedDates.Add(other.Date);
                }
                else
                {
                    await transactionRepository.AddAsync(transaction, cancellationToken);

                    if (item.RejectedCandidateId is { } rejectedId)
                    {
                        transferRepository.AddRejection(
                            TransferRejection.Create(request.UserId, transaction.Id, rejectedId));
                        await transferRepository.SaveChangesAsync(cancellationToken);
                    }
                }

                importTransaction.TransactionId = transaction.Id;
                imported++;
            }
            catch
            {
                errors++;
            }
        }

        bankImport.Imported = imported;
        bankImport.Errors = errors;

        // Com erro e nada importado o usuário ainda pode confirmar de novo; o extrato só vale para
        // a conciliação quando os lançamentos realmente passaram a existir na conta
        if (imported > 0 || errors == 0)
            bankImport.ConfirmedAt = DateTime.UtcNow;

        await bankImportRepository.UpdateAsync(bankImport, cancellationToken);

        // Só a conversão de lançamentos existentes muda números que já estavam no dashboard
        if (touchedDates.Count > 0)
            await dashboardCacheInvalidator.InvalidateAsync(request.UserId, touchedDates, cancellationToken);

        return new BankImportDto(
            Id: bankImport.Id,
            FileName: bankImport.FileName,
            Status: bankImport.Status,
            TotalRecords: bankImport.TotalRecords,
            Imported: bankImport.Imported,
            Duplicates: bankImport.Duplicates,
            Errors: bankImport.Errors,
            ErrorMessage: bankImport.ErrorMessage,
            CreatedAt: bankImport.CreatedAt,
            AccountId: bankImport.AccountId,
            AccountName: bankImport.Account?.Name);
    }

    private readonly record struct LinkDecision(
        Transaction? Candidate,
        TransferDirection? Direction,
        bool ForceUnpairedTransfer,
        Guid? RejectedCandidateId);

    /// <summary>
    /// Decide o que fazer com o par encontrado pelo pareamento:
    /// vincular (automático, ou sugestão aceita), importar sem par ou ignorar o par.
    /// </summary>
    private async Task<LinkDecision> DecideTransferLinkAsync(
        BankImportTransaction row,
        ConfirmImportItemDto selection,
        Dictionary<Guid, Transaction> candidates,
        HashSet<Guid> usedCandidates,
        Guid importAccountId,
        CancellationToken cancellationToken)
    {
        if (row.TransferMatchTransactionId is not { } candidateId || row.TransferMatchStatus is null)
            return default;

        var explicitLink = string.Equals(selection.TransferAction, TransferActions.Link, StringComparison.OrdinalIgnoreCase);
        var explicitIgnore = string.Equals(selection.TransferAction, TransferActions.Ignore, StringComparison.OrdinalIgnoreCase);

        var wantsLink = explicitLink ||
                        (!explicitIgnore && row.TransferMatchStatus == TransferMatchStatus.Auto);

        if (!wantsLink)
            return new LinkDecision(null, null, false, explicitIgnore ? candidateId : null);

        var direction = TransferMatchingService.DirectionOf(row.Type, null);

        // A outra ponta pode ter mudado desde o preview: revalida antes de vincular
        if (direction is not null &&
            candidates.TryGetValue(candidateId, out var candidate) &&
            !usedCandidates.Contains(candidateId) &&
            candidate.AccountId != importAccountId &&
            candidate.Amount == row.Amount &&
            TransferMatchingService.DirectionOf(candidate.Type, candidate.TransferDirection) is { } candidateDirection &&
            candidateDirection != direction &&
            !await transferRepository.IsLinkedAsync(candidateId, cancellationToken))
        {
            return new LinkDecision(candidate, direction, false, null);
        }

        // Quem pediu o vínculo explicitamente quer uma transferência: fica como ponta sem par (aparece nas pendências).
        // Já o vínculo automático que deixou de valer volta a ser um lançamento comum.
        return new LinkDecision(null, null, explicitLink, null);
    }

    private async Task<Dictionary<Guid, Transaction>> LoadCandidatesAsync(
        BankImport bankImport,
        Dictionary<Guid, ConfirmImportItemDto> selections,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var ids = bankImport.Transactions
            .Where(t => t.TransferMatchTransactionId.HasValue && selections.ContainsKey(t.Id))
            .Select(t => t.TransferMatchTransactionId!.Value)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
            return [];

        return (await transferRepository.GetTrackedTransactionsAsync(userId, ids, cancellationToken))
            .ToDictionary(t => t.Id);
    }

    /// <summary>
    /// Registra as duas pontas e o vínculo. Se a outra ponta era receita/despesa, ela vira transferência
    /// (a direção segue o tipo original; a categoria passa a ser a padrão de transferência).
    /// </summary>
    private void LinkPair(
        Guid userId,
        Transaction newTransaction,
        Transaction other,
        BankImportTransaction row)
    {
        if (other.Type != TransactionType.Transfer)
        {
            other.TransferDirection = TransferMatchingService.DirectionOf(other.Type, null);
            other.Type = TransactionType.Transfer;
            other.CategoryId = WellKnownIds.TransferCategoryId;
            other.SubcategoryId = null;
            other.UpdatedAt = DateTime.UtcNow;
        }

        transferRepository.AddTransaction(newTransaction);

        var newIsOut = newTransaction.TransferDirection == TransferDirection.Out;

        transferRepository.AddLink(new TransferLink
        {
            UserId = userId,
            OutTransactionId = newIsOut ? newTransaction.Id : other.Id,
            InTransactionId = newIsOut ? other.Id : newTransaction.Id,
            Origin = row.TransferMatchStatus == TransferMatchStatus.Auto
                ? TransferLinkOrigin.Auto
                : TransferLinkOrigin.Suggested,
            Score = row.TransferMatchScore,
        });
    }

    private static TransactionType ResolveType(string? typeFromRequest, TransactionType fallback)
    {
        return typeFromRequest switch
        {
            "Income" => TransactionType.Income,
            "Expense" => TransactionType.Expense,
            "Transfer" => TransactionType.Transfer,
            _ => fallback,
        };
    }
}
