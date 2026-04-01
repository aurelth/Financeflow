using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Reports.Commands.DeleteReport;

public class DeleteReportCommandHandler(
    IReportRepository reportRepository)
    : IRequestHandler<DeleteReportCommand>
{
    public async Task Handle(
        DeleteReportCommand request,
        CancellationToken cancellationToken)
    {
        var report = await reportRepository.GetByIdAsync(
            request.ReportId, request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Report), request.ReportId);

        // Remove o arquivo CSV se existir
        if (!string.IsNullOrEmpty(report.FilePath) && File.Exists(report.FilePath))
            File.Delete(report.FilePath);

        await reportRepository.DeleteAsync(report, cancellationToken);
    }
}
