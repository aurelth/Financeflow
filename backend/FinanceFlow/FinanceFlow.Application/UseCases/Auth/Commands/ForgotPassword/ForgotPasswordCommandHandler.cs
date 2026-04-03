using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace FinanceFlow.Application.UseCases.Auth.Commands.ForgotPassword;

public class ForgotPasswordCommandHandler(
    IUserRepository userRepository,
    IPasswordResetTokenRepository passwordResetTokenRepository,
    IEmailService emailService,
    IConfiguration configuration
) : IRequestHandler<ForgotPasswordCommand>
{
    public async Task Handle(
        ForgotPasswordCommand request,
        CancellationToken cancellationToken)
    {
        // Busca o usuário — responde com sucesso mesmo se não encontrado
        // para não revelar se o email existe no sistema
        var user = await userRepository.GetByEmailAsync(
            request.Email, cancellationToken);

        if (user is null)
            return;

        // Invalida tokens anteriores do usuário
        await passwordResetTokenRepository.InvalidateUserTokensAsync(
            user.Id, cancellationToken);

        // Gera novo token seguro
        var tokenValue = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");

        var resetToken = new PasswordResetToken
        {
            UserId = user.Id,
            Token = tokenValue,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = false
        };

        await passwordResetTokenRepository.AddAsync(resetToken, cancellationToken);

        // Monta o link de redefinição
        var frontendUrl = configuration["Frontend:BaseUrl"] ?? "http://localhost:3000";
        var resetLink = $"{frontendUrl}/reset-password?token={tokenValue}";

        // Envia o email
        await emailService.SendPasswordResetEmailAsync(
            user.Email,
            user.Name,
            resetLink,
            cancellationToken);
    }
}
