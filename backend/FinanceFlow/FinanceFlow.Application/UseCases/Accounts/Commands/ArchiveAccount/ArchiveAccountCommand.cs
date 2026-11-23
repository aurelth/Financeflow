using MediatR;

namespace FinanceFlow.Application.UseCases.Accounts.Commands.ArchiveAccount;

/// <summary>Arquiva a conta (não apaga, para preservar o histórico).</summary>
public record ArchiveAccountCommand(
    Guid Id,
    Guid UserId
) : IRequest;
