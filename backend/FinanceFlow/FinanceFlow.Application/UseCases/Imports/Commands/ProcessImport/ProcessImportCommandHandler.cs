using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Imports.Commands.ProcessImport;

public class ProcessImportCommandHandler(
    IBankImportRepository bankImportRepository)
    : IRequestHandler<ProcessImportCommand>
{
    public async Task Handle(
        ProcessImportCommand request,
        CancellationToken cancellationToken)
    {
        var bankImport = await bankImportRepository.GetByIdAsync(
            request.ImportId, request.UserId, cancellationToken);

        if (bankImport is null)
            return;

        bankImport.Status = BankImportStatus.Processing;
        await bankImportRepository.UpdateAsync(bankImport, cancellationToken);

        var duplicates = 0;

        // Verifica duplicatas por hash
        foreach (var t in bankImport.Transactions)
        {
            var isDuplicate = await bankImportRepository.HashExistsAsync(
                request.UserId, t.Hash, cancellationToken);

            if (isDuplicate)
            {
                t.IsDuplicate = true;
                t.IsSelected = false;
                duplicates++;
            }
        }

        // Remove automaticamente as BankImportTransactions duplicadas
        var duplicateTransactions = bankImport.Transactions
            .Where(t => t.IsDuplicate == true)
            .ToList();

        foreach (var t in duplicateTransactions)
            bankImport.Transactions.Remove(t);

        bankImport.Duplicates = duplicates;
        bankImport.Status = BankImportStatus.Completed;
        await bankImportRepository.UpdateAsync(bankImport, cancellationToken);
    }
}
