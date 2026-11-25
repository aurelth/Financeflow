using FinanceFlow.Application.DTOs.WeeklySummary;

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

    /// <summary>
    /// Envia o resumo semanal. Lança exceção se o provedor recusar o envio, para o chamador tentar de novo mais tarde.
    /// </summary>
    Task SendWeeklySummaryEmailAsync(
        string toEmail,
        string toName,
        WeeklySummaryDto summary,
        string dashboardUrl,
        string settingsUrl,
        CancellationToken cancellationToken = default);
}
