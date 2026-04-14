using MediatR;

namespace FinanceFlow.Application.UseCases.Imports.Commands.DeleteImport;

public record DeleteImportCommand(
    Guid ImportId,
    Guid UserId
) : IRequest;
