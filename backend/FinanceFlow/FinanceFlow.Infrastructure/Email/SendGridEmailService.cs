using System.Net;
using System.Text;
using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.DTOs.WeeklySummary;
using FinanceFlow.Application.Services;
using FinanceFlow.Domain.Entities;
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

    public async Task SendWeeklySummaryEmailAsync(
        string toEmail,
        string toName,
        WeeklySummaryDto summary,
        string dashboardUrl,
        string settingsUrl,
        CancellationToken cancellationToken = default)
    {
        var apiKey = configuration["SendGrid:ApiKey"]
            ?? throw new InvalidOperationException("SendGrid:ApiKey não configurado.");
        var fromEmail = configuration["SendGrid:FromEmail"]
            ?? throw new InvalidOperationException("SendGrid:FromEmail não configurado.");
        var fromName = configuration["SendGrid:FromName"] ?? "FinanceFlow";

        var client = new SendGridClient(apiKey);
        var message = MailHelper.CreateSingleEmail(
            new EmailAddress(fromEmail, fromName),
            new EmailAddress(toEmail, toName),
            WeeklySummaryText.Subject(summary),
            BuildWeeklySummaryPlainText(toName, summary, dashboardUrl, settingsUrl),
            BuildWeeklySummaryHtml(toName, summary, dashboardUrl, settingsUrl));

        var response = await client.SendEmailAsync(message, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "Erro ao enviar resumo semanal para {Email}: {StatusCode}",
                toEmail, response.StatusCode);

            // Ao contrário do e-mail de senha, aqui a falha sobe: o Worker só marca o resumo como enviado se der certo
            throw new InvalidOperationException(
                $"O SendGrid recusou o envio do resumo semanal ({(int)response.StatusCode}).");
        }

        logger.LogInformation("Resumo semanal enviado para {Email}", toEmail);
    }

    private static string BuildWeeklySummaryPlainText(
        string toName, WeeklySummaryDto s, string dashboardUrl, string settingsUrl)
    {
        var text = new StringBuilder();
        text.AppendLine($"Olá, {toName}! Seu resumo da semana ({WeeklySummaryText.Period(s)}):");
        text.AppendLine();
        text.AppendLine($"Entrou: {WeeklySummaryText.Money(s.Income, s.Currency)}");
        text.AppendLine($"Saiu: {WeeklySummaryText.Money(s.Expense, s.Currency)}");
        text.AppendLine($"Saldo: {WeeklySummaryText.Money(s.Balance, s.Currency)}");

        foreach (var c in s.TopCategories)
            text.AppendLine($"- {c.Name}: {WeeklySummaryText.Money(c.Amount, s.Currency)}");

        if (s.Upcoming.Items.Count > 0)
        {
            text.AppendLine();
            text.AppendLine("Próximos 7 dias:");
            foreach (var item in s.Upcoming.Items)
                text.AppendLine($"- {item.Date:dd/MM} {item.Description}: {WeeklySummaryText.Money(item.Amount, s.Currency)}");
        }

        if (s.Overdue.Count > 0)
            text.AppendLine($"\nContas vencidas: {s.Overdue.Count} ({WeeklySummaryText.Money(s.Overdue.Amount, s.Currency)})");

        foreach (var b in s.BudgetAlerts)
            text.AppendLine($"Orçamento de {b.CategoryName}: {b.Percentage:0}% usado");

        text.AppendLine();
        text.AppendLine($"Abrir o FinanceFlow: {dashboardUrl}");
        text.AppendLine($"Escolher quais avisos receber: {settingsUrl}");
        return text.ToString();
    }

    private static string BuildWeeklySummaryHtml(
        string toName, WeeklySummaryDto s, string dashboardUrl, string settingsUrl)
    {
        static string E(string value) => WebUtility.HtmlEncode(value);
        string M(decimal value) => E(WeeklySummaryText.Money(value, s.Currency));

        string Delta(decimal current, decimal previous)
        {
            var variation = WeeklySummaryText.Variation(current, previous);
            return variation is null
                ? string.Empty
                : $"<div style=\"color:#64748b;font-size:12px;\">{E(variation)} vs. semana anterior</div>";
        }

        var balanceColor = s.Balance >= 0 ? "#16a34a" : "#dc2626";
        var html = new StringBuilder();

        html.Append($$"""
            <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; color:#0f172a;">
              <h2 style="color:#6366f1;margin-bottom:4px;">Seu resumo da semana</h2>
              <p style="color:#64748b;margin-top:0;">{{E(WeeklySummaryText.Period(s))}}</p>
              <p>Olá, <strong>{{E(toName)}}</strong>! Veja como foi a sua semana e o que vem pela frente.</p>
              <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="margin:16px 0;">
                <tr>
                  <td style="padding:12px;background:#f1f5f9;border-radius:8px;width:33%;vertical-align:top;">
                    <div style="color:#64748b;font-size:12px;">Entrou</div>
                    <div style="font-size:18px;font-weight:bold;color:#16a34a;">{{M(s.Income)}}</div>
                    {{Delta(s.Income, s.PreviousIncome)}}
                  </td>
                  <td width="8"></td>
                  <td style="padding:12px;background:#f1f5f9;border-radius:8px;width:33%;vertical-align:top;">
                    <div style="color:#64748b;font-size:12px;">Saiu</div>
                    <div style="font-size:18px;font-weight:bold;color:#dc2626;">{{M(s.Expense)}}</div>
                    {{Delta(s.Expense, s.PreviousExpense)}}
                  </td>
                  <td width="8"></td>
                  <td style="padding:12px;background:#f1f5f9;border-radius:8px;width:33%;vertical-align:top;">
                    <div style="color:#64748b;font-size:12px;">Saldo</div>
                    <div style="font-size:18px;font-weight:bold;color:{{balanceColor}};">{{M(s.Balance)}}</div>
                  </td>
                </tr>
              </table>
            """);

        if (s.TopCategories.Count > 0)
        {
            html.Append("<h3 style=\"margin-bottom:8px;\">Onde o dinheiro foi</h3><table role=\"presentation\" width=\"100%\" cellspacing=\"0\" cellpadding=\"0\">");
            foreach (var c in s.TopCategories)
            {
                html.Append($"<tr><td style=\"padding:6px 0;border-bottom:1px solid #e2e8f0;\">{E(c.Icon)} {E(c.Name)} <span style=\"color:#64748b;font-size:12px;\">({c.Percentage:0}%)</span></td>" +
                            $"<td align=\"right\" style=\"padding:6px 0;border-bottom:1px solid #e2e8f0;font-weight:bold;\">{M(c.Amount)}</td></tr>");
            }
            html.Append("</table>");
        }

        if (s.Upcoming.Items.Count > 0)
        {
            html.Append("<h3 style=\"margin-bottom:8px;\">Nos próximos 7 dias</h3><table role=\"presentation\" width=\"100%\" cellspacing=\"0\" cellpadding=\"0\">");
            foreach (var item in s.Upcoming.Items)
            {
                var color = item.Type == TransactionType.Income ? "#16a34a" : "#0f172a";
                html.Append($"<tr><td style=\"padding:6px 0;border-bottom:1px solid #e2e8f0;\"><span style=\"color:#64748b;\">{item.Date:dd/MM}</span> {E(item.Description)}</td>" +
                            $"<td align=\"right\" style=\"padding:6px 0;border-bottom:1px solid #e2e8f0;font-weight:bold;color:{color};\">{M(item.Amount)}</td></tr>");
            }
            html.Append("</table>");

            if (s.Upcoming.ExpenseCount > 0)
                html.Append($"<p style=\"color:#64748b;font-size:13px;\">Total a pagar: <strong>{M(s.Upcoming.ExpenseAmount)}</strong> em {s.Upcoming.ExpenseCount} lançamento(s).</p>");
        }

        if (s.Overdue.Count > 0)
        {
            html.Append($"<p style=\"background:#fef2f2;color:#991b1b;padding:12px;border-radius:8px;\">⚠️ Você tem <strong>{s.Overdue.Count}</strong> conta(s) vencida(s) e não paga(s), somando <strong>{M(s.Overdue.Amount)}</strong>.</p>");
        }

        foreach (var b in s.BudgetAlerts)
        {
            html.Append($"<p style=\"background:#fffbeb;color:#92400e;padding:12px;border-radius:8px;\">📊 O orçamento de <strong>{E(b.CategoryName)}</strong> está em <strong>{b.Percentage:0}%</strong> ({M(b.SpentAmount)} de {M(b.LimitAmount)}).</p>");
        }

        html.Append($$"""
              <div style="text-align:center;margin:32px 0;">
                <a href="{{E(dashboardUrl)}}"
                   style="background-color:#6366f1;color:white;padding:14px 28px;text-decoration:none;border-radius:8px;font-weight:bold;">
                  Abrir o FinanceFlow
                </a>
              </div>
              <p style="color:#64748b;font-size:12px;">
                Você recebe este resumo toda segunda-feira. Para deixar de recebê-lo ou escolher outros avisos,
                <a href="{{E(settingsUrl)}}" style="color:#6366f1;">ajuste suas notificações</a>.
              </p>
            </div>
            """);

        return html.ToString();
    }
}
