using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.DTOs.Reports;
using FinanceFlow.Application.UseCases.Analytics.Queries.GetProjections;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Analytics;

public class GetProjectionsQueryHandlerTests
{
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<ICacheService> _cache = new();

    private GetProjectionsQueryHandler CreateHandler() =>
        new(_transactionRepository.Object, _cache.Object);

    private void SetupCache() =>
        _cache.Setup(c => c.GetOrSetAsync(
            It.IsAny<string>(),
            It.IsAny<Func<Task<ProjectionsDto>>>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()))
        .Returns<string, Func<Task<ProjectionsDto>>, TimeSpan, CancellationToken>(
            (_, factory, _, _) => factory());

    private void SetupTransactions(IEnumerable<Transaction> transactions) =>
        _transactionRepository
            .Setup(r => r.GetPagedByUserAsync(
                It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                It.IsAny<Guid?>(), It.IsAny<Guid?>(),
                It.IsAny<TransactionType?>(), It.IsAny<TransactionStatus?>(),
                It.IsAny<decimal?>(), It.IsAny<decimal?>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((transactions, transactions.Count()));

    [Fact]
    public async Task Handle_DeveRetornarHistoricoCorreto_QuandoMonthsBack6()
    {
        // Arrange
        SetupCache();
        SetupTransactions([]);

        var query = new GetProjectionsQuery(Guid.NewGuid(), MonthsBack: 6, MonthsAhead: 3);

        // Act
        var result = await CreateHandler().Handle(query, default);

        // Assert
        result.Historical.Should().HaveCount(6);
        result.MonthsAnalysed.Should().Be(6);
    }

    [Fact]
    public async Task Handle_DeveRetornarProjeccoesCorretas_QuandoMonthsAhead3()
    {
        // Arrange
        SetupCache();
        SetupTransactions([]);

        var query = new GetProjectionsQuery(Guid.NewGuid(), MonthsBack: 6, MonthsAhead: 3);

        // Act
        var result = await CreateHandler().Handle(query, default);

        // Assert
        result.Projected.Should().HaveCount(3);
        result.MonthsAhead.Should().Be(3);
    }

    [Fact]
    public async Task Handle_DeveMarcarHistoricoComoNaoProjectado()
    {
        // Arrange
        SetupCache();
        SetupTransactions([]);

        var query = new GetProjectionsQuery(Guid.NewGuid(), MonthsBack: 6, MonthsAhead: 3);

        // Act
        var result = await CreateHandler().Handle(query, default);

        // Assert
        result.Historical.Should().AllSatisfy(h => h.IsProjected.Should().BeFalse());
    }

    [Fact]
    public async Task Handle_DeveMarcarProjeccoesComoProjectado()
    {
        // Arrange
        SetupCache();
        SetupTransactions([]);

        var query = new GetProjectionsQuery(Guid.NewGuid(), MonthsBack: 6, MonthsAhead: 3);

        // Act
        var result = await CreateHandler().Handle(query, default);

        // Assert
        result.Projected.Should().AllSatisfy(p => p.IsProjected.Should().BeTrue());
    }

    [Fact]
    public async Task Handle_DeveRetornarProjeccoesNaoNegativas_QuandoNaoHaHistorico()
    {
        // Arrange
        SetupCache();
        SetupTransactions([]);

        var query = new GetProjectionsQuery(Guid.NewGuid(), MonthsBack: 6, MonthsAhead: 3);

        // Act
        var result = await CreateHandler().Handle(query, default);

        // Assert
        result.Projected.Should().AllSatisfy(p =>
        {
            p.Income.Should().BeGreaterThanOrEqualTo(0);
            p.Expenses.Should().BeGreaterThanOrEqualTo(0);
        });
    }

    [Fact]
    public async Task Handle_DeveCalcularProjeccoesComDadosHistoricos()
    {
        // Arrange
        SetupCache();

        // Gera 6 meses de histórico consistente
        var today = DateTime.UtcNow;
        var transactions = new List<Transaction>();

        for (var i = 6; i >= 1; i--)
        {
            var date = new DateTime(today.Year, today.Month, 1).AddMonths(-i);
            transactions.Add(new Transaction
            {
                Type = TransactionType.Income,
                Amount = 5000,
                Date = date,
                Status = TransactionStatus.Paid,
                Category = new Category()
            });
            transactions.Add(new Transaction
            {
                Type = TransactionType.Expense,
                Amount = 3000,
                Date = date,
                Status = TransactionStatus.Paid,
                Category = new Category()
            });
        }

        SetupTransactions(transactions);

        var query = new GetProjectionsQuery(Guid.NewGuid(), MonthsBack: 6, MonthsAhead: 3);

        // Act
        var result = await CreateHandler().Handle(query, default);

        // Assert — com histórico consistente, projecções devem ser positivas
        result.Projected.Should().AllSatisfy(p =>
        {
            p.Income.Should().BeGreaterThan(0);
            p.Expenses.Should().BeGreaterThan(0);
        });
    }
}
