using MediatR;

namespace FinanceFlow.Application.UseCases.Auth.Commands.ResetPassword;

public record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword
) : IRequest;
