using MediatR;

namespace FinanceFlow.Application.UseCases.Reports.Commands.DeleteReport;

public record DeleteReportCommand(
    Guid ReportId,
    Guid UserId
) : IRequest;
