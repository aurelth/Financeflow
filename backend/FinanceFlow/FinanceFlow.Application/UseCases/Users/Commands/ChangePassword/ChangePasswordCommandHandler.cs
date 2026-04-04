using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.UseCases.Auth.Commands.ResetPassword;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Users.Commands.ChangePassword;

public class ChangePasswordCommandHandler(
    IUserRepository userRepository,
    IPasswordService passwordService
) : IRequestHandler<ChangePasswordCommand>
{
    public async Task Handle(
        ChangePasswordCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(
            request.UserId, cancellationToken)
            ?? throw new NotFoundException("Utilizador", request.UserId);

        // Valida senha atual
        if (!passwordService.Verify(request.CurrentPassword, user.PasswordHash))
            throw new ValidationException(
                "A senha atual está incorreta.",
                new Dictionary<string, string[]>
                {
                    { "CurrentPassword", ["A senha atual está incorreta."] }
                });

        user.PasswordHash = passwordService.Hash(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await userRepository.UpdateAsync(user, cancellationToken);
    }
}
