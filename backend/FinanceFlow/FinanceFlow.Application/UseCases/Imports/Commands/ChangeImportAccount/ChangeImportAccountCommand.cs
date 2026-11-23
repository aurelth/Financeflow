using FinanceFlow.Application.DTOs.Imports;
using MediatR;

namespace FinanceFlow.Application.UseCases.Imports.Commands.ChangeImportAccount;

public record ChangeImportAccountCommand(
    Guid ImportId,
    Guid UserId,
    Guid AccountId
) : IRequest<BankImportDto>;
