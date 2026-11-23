using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.DTOs.Imports;
using FinanceFlow.Domain.Constants;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Imports.Commands.ConfirmImport;

public class ConfirmImportCommandHandler(
    IBankImportRepository bankImportRepository,
    ITransactionRepository transactionRepository,
    ICategoryRepository categoryRepository)
    : IRequestHandler<ConfirmImportCommand, BankImportDto>
{
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

        // 1) Valida tudo antes de gravar qualquer coisa: linha sem categoria nunca some em silêncio
        var plan = new List<(BankImportTransaction Row, TransactionType Type, Guid CategoryId)>();
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

            var transactionType = ResolveType(selection.Type, importTransaction.Type);
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

            plan.Add((importTransaction, transactionType, categoryId));
        }

        if (rowErrors.Count > 0)
            throw new ValidationException(
                $"{rowErrors.Count} lançamento(s) sem categoria válida. Escolha a categoria ou desmarque a linha para confirmar.",
                rowErrors);

        // 2) Grava
        var imported = 0;
        var errors = 0;

        foreach (var (importTransaction, transactionType, categoryId) in plan)
        {
            try
            {
                importTransaction.SuggestedCategoryId = categoryId;

                var transaction = new Transaction
                {
                    UserId = request.UserId,
                    AccountId = bankImport.AccountId,
                    Amount = importTransaction.Amount,
                    Type = transactionType,
                    Date = importTransaction.Date,
                    Description = importTransaction.Description,
                    Status = TransactionStatus.Paid,
                    CategoryId = categoryId,
                    ImportHash = importTransaction.Hash,
                    ExternalId = string.IsNullOrWhiteSpace(importTransaction.ExternalId)
                        ? null
                        : importTransaction.ExternalId,
                    Tags = "[]",
                };

                await transactionRepository.AddAsync(transaction, cancellationToken);

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

        await bankImportRepository.UpdateAsync(bankImport, cancellationToken);

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
