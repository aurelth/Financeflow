using FinanceFlow.Application.DTOs;
using MediatR;

namespace FinanceFlow.Application.UseCases.Reports.Commands.RequestReport;

public record RequestReportCommand(
    Guid UserId,
    int Month,
    int Year
) : IRequest<ReportDto>;
