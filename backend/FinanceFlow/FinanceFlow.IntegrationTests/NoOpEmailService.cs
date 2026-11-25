using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.DTOs.WeeklySummary;

namespace FinanceFlow.IntegrationTests;

public class NoOpEmailService : IEmailService
{
    public Task SendPasswordResetEmailAsync(
        string toEmail,
        string toName,
        string resetLink,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task SendWeeklySummaryEmailAsync(
        string toEmail,
        string toName,
        WeeklySummaryDto summary,
        string dashboardUrl,
        string settingsUrl,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
