namespace FinanceFlow.Application.Common.Interfaces;

public interface IEmailService
{
    /// <summary>
    /// Envia um email de redefinição de senha com o link contendo o token.
    /// </summary>
    Task SendPasswordResetEmailAsync(
        string toEmail,
        string toName,
        string resetLink,
        CancellationToken cancellationToken = default);
}
