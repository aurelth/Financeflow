using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.UseCases.Reports.Commands.DeleteReport;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Reports;

public class DeleteReportCommandHandlerTests
{
    private readonly Mock<IReportRepository> _reportRepository = new();

    private static readonly Guid ReportId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    private DeleteReportCommandHandler CreateHandler() =>
        new(_reportRepository.Object);

    [Fact]
    public async Task Handle_DeveDeletarRelatorio_QuandoExiste()
    {
        // Arrange
        var report = new Report
        {
            Id = ReportId,
            UserId = UserId,
            Type = ReportType.CSV,
            Status = ReportStatus.Completed,
            Month = 3,
            Year = 2026,
            FilePath = null, // sem arquivo físico para não tentar deletar
            FileName = null,
        };

        _reportRepository
            .Setup(r => r.GetByIdAsync(ReportId, UserId, default))
            .ReturnsAsync(report);

        _reportRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Report>(), default))
            .Returns(Task.CompletedTask);

        var command = new DeleteReportCommand(ReportId, UserId);

        // Act
        await CreateHandler().Handle(command, default);

        // Assert
        _reportRepository.Verify(r =>
            r.DeleteAsync(It.Is<Report>(rep => rep.Id == ReportId), default),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DeveLancarNotFoundException_QuandoRelatorioNaoExiste()
    {
        // Arrange
        _reportRepository
            .Setup(r => r.GetByIdAsync(ReportId, UserId, default))
            .ReturnsAsync((Report?)null);

        var command = new DeleteReportCommand(ReportId, UserId);

        // Act
        var act = async () => await CreateHandler().Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_DeveDeletarRelatorio_QuandoNaoTemArquivo()
    {
        // Arrange
        var report = new Report
        {
            Id = ReportId,
            UserId = UserId,
            Type = ReportType.CSV,
            Status = ReportStatus.Failed,
            Month = 3,
            Year = 2026,
            FilePath = null,
            FileName = null,
        };

        _reportRepository
            .Setup(r => r.GetByIdAsync(ReportId, UserId, default))
            .ReturnsAsync(report);

        _reportRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Report>(), default))
            .Returns(Task.CompletedTask);

        var command = new DeleteReportCommand(ReportId, UserId);

        // Act
        await CreateHandler().Handle(command, default);

        // Assert
        _reportRepository.Verify(r =>
            r.DeleteAsync(It.IsAny<Report>(), default),
            Times.Once);
    }
}
