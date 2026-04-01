using FinanceFlow.Application.UseCases.Reports.Queries.GetReports;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Reports;

public class GetReportsQueryHandlerTests
{
    private readonly Mock<IReportRepository> _reportRepository = new();

    private static readonly Guid UserId = Guid.NewGuid();

    private GetReportsQueryHandler CreateHandler() =>
        new(_reportRepository.Object);

    [Fact]
    public async Task Handle_DeveRetornarRelatoriosDoUsuario()
    {
        // Arrange
        var reports = new List<Report>
        {
            new()
            {
                Id        = Guid.NewGuid(),
                UserId    = UserId,
                Type      = ReportType.CSV,
                Status    = ReportStatus.Completed,
                Month     = 3,
                Year      = 2026,
                FileName  = "relatorio_2026_03.csv",
                CreatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id        = Guid.NewGuid(),
                UserId    = UserId,
                Type      = ReportType.CSV,
                Status    = ReportStatus.Pending,
                Month     = 2,
                Year      = 2026,
                CreatedAt = DateTime.UtcNow,
            },
        };

        _reportRepository
            .Setup(r => r.GetByUserAsync(UserId, default))
            .ReturnsAsync(reports);

        var query = new GetReportsQuery(UserId);

        // Act
        var result = (await CreateHandler().Handle(query, default)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].Status.Should().Be(ReportStatus.Completed);
        result[0].FileName.Should().Be("relatorio_2026_03.csv");
        result[1].Status.Should().Be(ReportStatus.Pending);
    }

    [Fact]
    public async Task Handle_DeveRetornarListaVaziaQuandoNaoHaRelatorios()
    {
        // Arrange
        _reportRepository
            .Setup(r => r.GetByUserAsync(UserId, default))
            .ReturnsAsync([]);

        var query = new GetReportsQuery(UserId);

        // Act
        var result = await CreateHandler().Handle(query, default);

        // Assert
        result.Should().BeEmpty();
    }
}
