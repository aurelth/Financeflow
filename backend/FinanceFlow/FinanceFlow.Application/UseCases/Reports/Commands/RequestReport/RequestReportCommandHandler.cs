using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Application.Events;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace FinanceFlow.Application.UseCases.Reports.Commands.RequestReport;

public class RequestReportCommandHandler(
    IReportRepository reportRepository,
    IEventPublisher eventPublisher,
    IConfiguration configuration)
    : IRequestHandler<RequestReportCommand, ReportDto>
{
    public async Task<ReportDto> Handle(
        RequestReportCommand request,
        CancellationToken cancellationToken)
    {
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
