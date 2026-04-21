using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.DTOs.HealthScore;
using FinanceFlow.Application.UseCases.HealthScore.Queries.GetHealthScore;
using FinanceFlow.Application.UseCases.HealthScore.Queries.GetHealthScoreHistory;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.HealthScore;

public class HealthScoreQueryHandlerTests
{
    private readonly Mock<IHealthScoreService> _healthScoreService = new();

    private static readonly Guid UserId = Guid.NewGuid();

    private static readonly HealthScoreResultDto ValidResult = new(
        Score: 75,
        Classification: "Bom",
        Details:
        [
            new ScoreDetailDto("Saldo do mês",               20, 25, "Saldo positivo."),
            new ScoreDetailDto("Controlo de orçamentos",     25, 25, "Todas as categorias dentro do limite."),
            new ScoreDetailDto("Regularidade de receitas",   20, 20, "Receitas registadas."),
            new ScoreDetailDto("Diversificação de despesas", 10, 15, "Gastos moderadamente concentrados."),
            new ScoreDetailDto("Transações agendadas",       15, 15, "Nenhuma transação em atraso."),
        ]);

    // GetHealthScoreQueryHandler

    [Fact]
    public async Task GetHealthScore_DeveRetornarResultado_QuandoServicoFunciona()
    {
        // Arrange
        _healthScoreService
            .Setup(s => s.CalculateAsync(UserId, 4, 2026, default))
            .ReturnsAsync(ValidResult);

        var handler = new GetHealthScoreQueryHandler(_healthScoreService.Object);
        var query = new GetHealthScoreQuery(UserId, 4, 2026);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        result.Should().NotBeNull();
        result.Score.Should().Be(75);
        result.Classification.Should().Be("Bom");
        result.Details.Should().HaveCount(5);

        _healthScoreService.Verify(s =>
            s.CalculateAsync(UserId, 4, 2026, default), Times.Once);
    }

    [Fact]
    public async Task GetHealthScore_DevePassarMesEAnoCorretos_QuandoChamadaRealizada()
    {
        // Arrange
        _healthScoreService
            .Setup(s => s.CalculateAsync(UserId, 1, 2026, default))
            .ReturnsAsync(ValidResult);

        var handler = new GetHealthScoreQueryHandler(_healthScoreService.Object);
        var query = new GetHealthScoreQuery(UserId, 1, 2026);

        // Act
        await handler.Handle(query, default);

        // Assert
        _healthScoreService.Verify(s =>
            s.CalculateAsync(UserId, 1, 2026, default), Times.Once);
    }

    // GetHealthScoreHistoryQueryHandler

    [Fact]
    public async Task GetHealthScoreHistory_DeveRetornar6Meses_QuandoChamadaRealizada()
    {
        // Arrange
        _healthScoreService
            .Setup(s => s.CalculateAsync(UserId, It.IsAny<int>(), It.IsAny<int>(), default))
            .ReturnsAsync(ValidResult);

        var handler = new GetHealthScoreHistoryQueryHandler(_healthScoreService.Object);
        var query = new GetHealthScoreHistoryQuery(UserId);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        result.Should().HaveCount(6);

        _healthScoreService.Verify(s =>
            s.CalculateAsync(UserId, It.IsAny<int>(), It.IsAny<int>(), default),
            Times.Exactly(6));
    }

    [Fact]
    public async Task GetHealthScoreHistory_DeveRetornarItensComCamposPreenchidos_QuandoChamadaRealizada()
    {
        // Arrange
        _healthScoreService
            .Setup(s => s.CalculateAsync(UserId, It.IsAny<int>(), It.IsAny<int>(), default))
            .ReturnsAsync(ValidResult);

        var handler = new GetHealthScoreHistoryQueryHandler(_healthScoreService.Object);
        var query = new GetHealthScoreHistoryQuery(UserId);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        result.Should().OnlyContain(h =>
            h.Score == 75 &&
            h.Classification == "Bom" &&
            h.Month >= 1 &&
            h.Month <= 12 &&
            !string.IsNullOrEmpty(h.MonthLabel));
    }
}
