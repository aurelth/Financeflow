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
    }

    private RequestReportCommandHandler CreateHandler() =>
        new(_reportRepository.Object, _eventPublisher.Object, _configuration.Object);

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
}
