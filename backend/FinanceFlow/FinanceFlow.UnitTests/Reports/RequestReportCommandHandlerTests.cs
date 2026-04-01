using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.UseCases.Reports.Commands.RequestReport;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;

namespace FinanceFlow.UnitTests.Reports;

public class RequestReportCommandHandlerTests
{
    private readonly Mock<IReportRepository> _reportRepository = new();
    private readonly Mock<IEventPublisher> _eventPublisher = new();
    private readonly Mock<IConfiguration> _configuration = new();
    private readonly Mock<ITransactionRepository> _transactionRepository = new();

    private static readonly Guid UserId = Guid.NewGuid();

    public RequestReportCommandHandlerTests()
    {
        _configuration
            .Setup(c => c["Kafka:Topics:ReportsRequested"])
            .Returns("finance.reports.requested");

        _eventPublisher
            .Setup(e => e.PublishAsync(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Por padrão não há relatório concluído anterior
        _reportRepository
            .Setup(r => r.GetLastCompletedAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Report?)null);

        // Por padrão há mudanças nas transações
        _transactionRepository
            .Setup(t => t.HasChangedSinceAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    private RequestReportCommandHandler CreateHandler() =>
        new(_reportRepository.Object,
            _transactionRepository.Object,
            _eventPublisher.Object,
            _configuration.Object);

    [Fact]
    public async Task Handle_DeveCriarReportComStatusPending()
    {
        // Arrange
        var command = new RequestReportCommand(UserId, Month: 3, Year: 2026);

        _reportRepository
            .Setup(r => r.AddAsync(It.IsAny<Report>(), default))
            .Returns(Task.CompletedTask);

        // Act
        var result = await CreateHandler().Handle(command, default);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(ReportStatus.Pending);
        result.Month.Should().Be(3);
        result.Year.Should().Be(2026);

        _reportRepository.Verify(r =>
            r.AddAsync(It.Is<Report>(rep =>
                rep.UserId == UserId &&
                rep.Status == ReportStatus.Pending &&
                rep.Type == ReportType.CSV),
            default), Times.Once);
    }

    [Fact]
    public async Task Handle_DevePublicarEventoKafka()
    {
        // Arrange
        var command = new RequestReportCommand(UserId, Month: 3, Year: 2026);

        _reportRepository
            .Setup(r => r.AddAsync(It.IsAny<Report>(), default))
            .Returns(Task.CompletedTask);

        // Act
        await CreateHandler().Handle(command, default);

        // Assert
        _eventPublisher.Verify(e =>
            e.PublishAsync(
                "finance.reports.requested",
                It.IsAny<object>(),
                default),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DeveLancarValidationException_QuandoJaExisteRelatorioSemMudancas()
    {
        // Arrange
        var lastReport = new Report
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            Status = ReportStatus.Completed,
            Month = 3,
            Year = 2026,
            CompletedAt = DateTime.UtcNow.AddHours(-1),
        };

        _reportRepository
            .Setup(r => r.GetLastCompletedAsync(UserId, 3, 2026, default))
            .ReturnsAsync(lastReport);

        _transactionRepository
            .Setup(t => t.HasChangedSinceAsync(UserId, 3, 2026, It.IsAny<DateTime>(), default))
            .ReturnsAsync(false);

        var command = new RequestReportCommand(UserId, Month: 3, Year: 2026);

        // Act
        var act = async () => await CreateHandler().Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<Application.Common.Exceptions.ValidationException>();
    }

    [Fact]
    public async Task Handle_DevePermitirNovoRelatorio_QuandoHouverMudancasNasTransacoes()
    {
        // Arrange
        var lastReport = new Report
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            Status = ReportStatus.Completed,
            Month = 3,
            Year = 2026,
            CompletedAt = DateTime.UtcNow.AddHours(-1),
        };

        _reportRepository
            .Setup(r => r.GetLastCompletedAsync(UserId, 3, 2026, default))
            .ReturnsAsync(lastReport);

        _transactionRepository
            .Setup(t => t.HasChangedSinceAsync(UserId, 3, 2026, It.IsAny<DateTime>(), default))
            .ReturnsAsync(true);

        _reportRepository
            .Setup(r => r.AddAsync(It.IsAny<Report>(), default))
            .Returns(Task.CompletedTask);

        var command = new RequestReportCommand(UserId, Month: 3, Year: 2026);

        // Act
        var result = await CreateHandler().Handle(command, default);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(ReportStatus.Pending);
    }
}
