using MediatR;

namespace FinanceFlow.Application.UseCases.Auth.Commands.ResetPassword;

public record ResetPasswordCommand(
    string Token,
    string NewPassword,
    string ConfirmPassword
) : IRequest;
