using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.DTOs.Imports;
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

            // Salta transações sem categoria em vez de contar como erro
            if (selection.CategoryId == Guid.Empty)
                continue;

            try
            {
                // Valida apenas se categoryId foi fornecido
                var category = await categoryRepository.GetByIdAsync(
                    selection.CategoryId, request.UserId, cancellationToken);

                if (category is null)
                {
                    errors++;
                    continue;
                }

                importTransaction.SuggestedCategoryId = selection.CategoryId;

                var transaction = new Transaction
                {
                    UserId = request.UserId,
                    Amount = importTransaction.Amount,
                    Type = importTransaction.Type,
                    Date = importTransaction.Date,
                    Description = importTransaction.Description,
                    Status = TransactionStatus.Paid,
                    CategoryId = selection.CategoryId,
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
}
