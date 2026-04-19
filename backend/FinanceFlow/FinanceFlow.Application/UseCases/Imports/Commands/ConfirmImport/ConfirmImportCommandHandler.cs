using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.DTOs.Imports;
using FinanceFlow.Domain.Constants;
using FinanceFlow.Domain.Entities;
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

        var imported = 0;
        var errors = 0;

        var selections = request.Request.Transactions
            .ToDictionary(t => t.Id);

        foreach (var importTransaction in bankImport.Transactions)
        {
            if (!selections.TryGetValue(importTransaction.Id, out var selection))
                continue;

            if (!selection.IsSelected)
                continue;

            if (importTransaction.IsDuplicate)
                continue;

            // Resolve o tipo enviado pelo frontend
            var transactionType = ResolveType(selection.Type, importTransaction.Type);

            // Categoria opcional para Transfer — usa categoria padrão
            var categoryId = selection.CategoryId;

            if (categoryId == Guid.Empty)
            {
                if (transactionType == TransactionType.Transfer)
                    categoryId = WellKnownIds.TransferCategoryId;
                else
                    continue; // Salta transações não-Transfer sem categoria
            }

            try
            {
                // Valida categoria apenas se não for a categoria padrão de transferência
                if (categoryId != WellKnownIds.TransferCategoryId)
                {
                    var category = await categoryRepository.GetByIdAsync(
                        categoryId, request.UserId, cancellationToken);

                    if (category is null)
                    {
                        errors++;
                        continue;
                    }
                }

                importTransaction.SuggestedCategoryId = categoryId;

                var transaction = new Transaction
                {
                    UserId = request.UserId,
                    Amount = importTransaction.Amount,
                    Type = transactionType,
                    Date = importTransaction.Date,
                    Description = importTransaction.Description,
                    Status = TransactionStatus.Paid,
                    CategoryId = categoryId,
                    ImportHash = importTransaction.Hash,
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
            CreatedAt: bankImport.CreatedAt);
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
