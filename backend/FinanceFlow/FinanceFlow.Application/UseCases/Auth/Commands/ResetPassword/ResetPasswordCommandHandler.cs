using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Auth.Commands.ResetPassword;

public class ResetPasswordCommandHandler(
    IPasswordResetTokenRepository passwordResetTokenRepository,
    IUserRepository userRepository,
    IPasswordService passwordService
) : IRequestHandler<ResetPasswordCommand>
{
    public async Task Handle(
        ResetPasswordCommand request,
        CancellationToken cancellationToken)
    {
        // Busca o token válido
        var resetToken = await passwordResetTokenRepository.GetValidTokenAsync(
            request.Token, cancellationToken)
            ?? throw new ValidationException("Token inválido ou expirado.");

        // Usa o User já carregado pelo Include — sem segunda query // corrigido
        var user = resetToken.User
            ?? throw new NotFoundException("Usuário não encontrado.");

        // Atualiza a senha
        user.PasswordHash = passwordService.Hash(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await userRepository.UpdateAsync(user, cancellationToken);

        // Marca o token como usado
        resetToken.IsUsed = true;
        resetToken.UpdatedAt = DateTime.UtcNow;

        await passwordResetTokenRepository.UpdateAsync(resetToken, cancellationToken);
    }
}
