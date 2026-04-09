using MediatR;

namespace FinanceFlow.Application.UseCases.Settings.Commands.DeleteAccount;

public record DeleteAccountCommand(
    Guid UserId,
    string CurrentPassword
) : IRequest;
