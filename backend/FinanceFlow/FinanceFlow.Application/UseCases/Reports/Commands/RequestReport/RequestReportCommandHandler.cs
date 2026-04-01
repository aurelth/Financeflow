using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Application.Events;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;

namespace FinanceFlow.Application.UseCases.Reports.Commands.RequestReport;

public class RequestReportCommandHandler(
    IReportRepository reportRepository,
    ITransactionRepository transactionRepository,
    IEventPublisher eventPublisher,
    IConfiguration configuration)
    : IRequestHandler<RequestReportCommand, ReportDto>
{
    public async Task<ReportDto> Handle(
        RequestReportCommand request,
        CancellationToken cancellationToken)
    {
        // Verifica se já existe um relatório concluído para o período
        var lastCompleted = await reportRepository.GetLastCompletedAsync(
            request.UserId, request.Month, request.Year, cancellationToken);

        if (lastCompleted is not null)
        {
            // Verifica se houve mudanças nas transações desde o último relatório
            var hasChanges = await transactionRepository.HasChangedSinceAsync(
                request.UserId,
                request.Month,
                request.Year,
                lastCompleted.CompletedAt!.Value,
                cancellationToken);

            if (!hasChanges)
                throw new FinanceFlow.Application.Common.Exceptions.ValidationException(
                    "Já existe um relatório gerado para este período sem alterações nas transações. " +
                    "Faça alterações nas transações antes de gerar um novo relatório.");
        }

        var report = new Report
        {
            UserId = request.UserId,
            Type = ReportType.CSV,
            Status = ReportStatus.Pending,
            Month = request.Month,
            Year = request.Year,
        };

        await reportRepository.AddAsync(report, cancellationToken);

        var topic = configuration["Kafka:Topics:ReportsRequested"]
                    ?? "finance.reports.requested";

        await eventPublisher.PublishAsync(topic, new ReportRequestedEvent(
            ReportId: report.Id,
            UserId: report.UserId,
            Month: report.Month,
            Year: report.Year,
            RequestedAt: report.CreatedAt),
            cancellationToken);

        return new ReportDto(
            Id: report.Id,
            Type: report.Type,
            Status: report.Status,
            Month: report.Month,
            Year: report.Year,
            FileName: report.FileName,
            CreatedAt: report.CreatedAt,
            CompletedAt: report.CompletedAt);
    }
}
