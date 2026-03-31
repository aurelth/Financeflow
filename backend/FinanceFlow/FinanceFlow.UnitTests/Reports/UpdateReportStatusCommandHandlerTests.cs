using FinanceFlow.Application.UseCases.Reports.Commands.UpdateReportStatus;
using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Reports;

public class UpdateReportStatusCommandHandlerTests
{
    private readonly Mock<IReportRepository> _reportRepository = new();

    private static readonly Guid ReportId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    private UpdateReportStatusCommandHandler CreateHandler() =>
        new(_reportRepository.Object);

    [Fact]
    public async Task Handle_DeveAtualizarStatusParaCompleted()
    {
        // Arrange
        var report = new Report
        {
            Id = ReportId,
            UserId = UserId,
            Type = ReportType.CSV,
            Status = ReportStatus.Processing,
            Month = 3,
            Year = 2026,
        };

        _reportRepository
            .Setup(r => r.GetByIdInternalAsync(ReportId, default))
            .ReturnsAsync(report);

        _reportRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Report>(), default))
            .Returns(Task.CompletedTask);

        var command = new UpdateReportStatusCommand(
            ReportId: ReportId,
            Status: "Completed",
            FilePath: "storage/reports/user/relatorio.csv",
            FileName: "relatorio.csv");

        // Act
        await CreateHandler().Handle(command, default);

        // Assert
        _reportRepository.Verify(r =>
            r.UpdateAsync(It.Is<Report>(rep =>
                rep.Status == ReportStatus.Completed &&
                rep.FilePath == "storage/reports/user/relatorio.csv" &&
                rep.FileName == "relatorio.csv" &&
                rep.CompletedAt != null),
            default), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveLancarNotFoundException_QuandoReportNaoExiste()
    {
        // Arrange
        _reportRepository
            .Setup(r => r.GetByIdInternalAsync(ReportId, default))
            .ReturnsAsync((Report?)null);

        var command = new UpdateReportStatusCommand(
            ReportId: ReportId,
            Status: "Completed",
            FilePath: null,
            FileName: null);

        // Act
        var act = async () => await CreateHandler().Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
