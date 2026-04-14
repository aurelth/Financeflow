using MediatR;

namespace FinanceFlow.Application.UseCases.Imports.Commands.ProcessImport;

public record ProcessImportCommand(
    Guid ImportId,
    Guid UserId
) : IRequest;
