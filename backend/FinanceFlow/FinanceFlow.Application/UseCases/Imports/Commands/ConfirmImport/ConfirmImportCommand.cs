using FinanceFlow.Application.DTOs.Imports;
using MediatR;

namespace FinanceFlow.Application.UseCases.Imports.Commands.ConfirmImport;

public record ConfirmImportCommand(
    Guid ImportId,
    Guid UserId,
    ConfirmImportRequestDto Request
) : IRequest<BankImportDto>;
