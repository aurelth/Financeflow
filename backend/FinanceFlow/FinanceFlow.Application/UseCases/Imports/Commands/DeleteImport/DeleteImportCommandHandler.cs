using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Imports.Commands.DeleteImport;

public class DeleteImportCommandHandler(
    IBankImportRepository bankImportRepository)
    : IRequestHandler<DeleteImportCommand>
{
    public async Task Handle(
        DeleteImportCommand request,
        CancellationToken cancellationToken)
    {
        var bankImport = await bankImportRepository.GetByIdAsync(
            request.ImportId, request.UserId, cancellationToken);

        if (bankImport is null)
            throw new NotFoundException("Importação não encontrada.");

        await bankImportRepository.DeleteAsync(
            request.ImportId, request.UserId, cancellationToken);
    }
}
