using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.UseCases.WeeklySummary.Queries.GetWeeklySummary;
using FinanceFlow.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace FinanceFlow.Application.UseCases.WeeklySummary.Commands.SendWeeklySummaryEmail;

public class SendWeeklySummaryEmailCommandHandler(
    IUserRepository userRepository,
    INotificationPreferencesRepository preferencesRepository,
    ISender sender,
    IEmailService emailService,
    IConfiguration configuration)
    : IRequestHandler<SendWeeklySummaryEmailCommand>
{
    public async Task Handle(SendWeeklySummaryEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("Usuário não encontrado.");

        // Defesa em profundidade: o Worker já filtra, mas quem desligou o e-mail nunca deve recebê-lo
        var preferences = await preferencesRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (preferences is { WeeklySummaryEmailEnabled: false })
            return;

        var summary = await sender.Send(new GetWeeklySummaryQuery(request.UserId), cancellationToken);
        if (!summary.HasActivity)
            return;

        var baseUrl = (configuration["Frontend:BaseUrl"] ?? "http://localhost:3000").TrimEnd('/');

        await emailService.SendWeeklySummaryEmailAsync(
            toEmail: user.Email,
            toName: user.Name,
            summary: summary,
            dashboardUrl: $"{baseUrl}/dashboard",
            settingsUrl: $"{baseUrl}/settings",
            cancellationToken: cancellationToken);
    }
}
