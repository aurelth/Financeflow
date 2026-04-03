using FinanceFlow.Application.Common.Interfaces;

namespace FinanceFlow.IntegrationTests;

public class NoOpEmailService : IEmailService
{
    public Task SendPasswordResetEmailAsync(
        string toEmail,
        string toName,
        string resetLink,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
