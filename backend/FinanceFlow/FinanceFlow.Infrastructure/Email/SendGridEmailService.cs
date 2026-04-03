using FinanceFlow.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Net.Mail;

namespace FinanceFlow.Infrastructure.Email;

public class SendGridEmailService(
    IConfiguration configuration,
    ILogger<SendGridEmailService> logger
) : IEmailService
{
    public async Task SendPasswordResetEmailAsync(
        string toEmail,
        string toName,
        string resetLink,
        CancellationToken cancellationToken = default)
    {
        var apiKey = configuration["SendGrid:ApiKey"]
            ?? throw new InvalidOperationException("SendGrid:ApiKey não configurado.");
        var fromEmail = configuration["SendGrid:FromEmail"]
            ?? throw new InvalidOperationException("SendGrid:FromEmail não configurado.");
        var fromName = configuration["SendGrid:FromName"] ?? "FinanceFlow";

        var client = new SendGridClient(apiKey);
        var from = new EmailAddress(fromEmail, fromName);
        var to = new EmailAddress(toEmail, toName);
        var subject = "Redefinição de senha — FinanceFlow";

        var htmlContent = $"""
            <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
                <h2 style="color: #6366f1;">Redefinição de senha</h2>
                <p>Olá, <strong>{toName}</strong>!</p>
                <p>Recebemos uma solicitação para redefinir a senha da sua conta no FinanceFlow.</p>
                <p>Clique no botão abaixo para criar uma nova senha. O link expira em <strong>1 hora</strong>.</p>
                <div style="text-align: center; margin: 32px 0;">
                    <a href="{resetLink}"
                       style="background-color: #6366f1; color: white; padding: 14px 28px;
                              text-decoration: none; border-radius: 8px; font-weight: bold;">
                        Redefinir senha
                    </a>
                </div>
                <p style="color: #64748b; font-size: 14px;">
                    Se você não solicitou a redefinição, ignore este email. Sua senha permanece a mesma.
                </p>
                <p style="color: #64748b; font-size: 14px;">
                    Por segurança, nunca compartilhe este link com ninguém.
                </p>
            </div>
            """;

        var plainContent = $"Olá {toName}, acesse o link para redefinir sua senha: {resetLink}";

        var message = MailHelper.CreateSingleEmail(
            from, to, subject, plainContent, htmlContent);

        var response = await client.SendEmailAsync(message, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "Erro ao enviar email de redefinição para {Email}: {StatusCode}",
                toEmail, response.StatusCode);
        }
        else
        {
            logger.LogInformation(
                "Email de redefinição enviado para {Email}", toEmail);
        }
    }
}
