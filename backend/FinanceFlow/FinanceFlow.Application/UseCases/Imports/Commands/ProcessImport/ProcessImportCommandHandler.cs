using FinanceFlow.Application.Services;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Imports.Commands.ProcessImport;

public class ProcessImportCommandHandler(
    IBankImportRepository bankImportRepository,
    IImportDeduplicationService deduplicationService,
    ITransferMatchingService transferMatchingService)
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

        // As duplicatas ficam visíveis no preview (desmarcadas): o usuário decide se importa mesmo assim
        bankImport.Duplicates = await deduplicationService.DetectAsync(bankImport, cancellationToken);

        // Depois das duplicatas: linhas já existentes não podem ser a ponta de uma transferência nova
        await transferMatchingService.MatchAsync(bankImport, cancellationToken);

        bankImport.Status = BankImportStatus.Completed;

        await bankImportRepository.UpdateAsync(bankImport, cancellationToken);
    }
}
