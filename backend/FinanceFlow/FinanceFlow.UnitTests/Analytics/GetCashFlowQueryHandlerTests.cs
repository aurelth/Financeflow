using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.DTOs.Reports;
using FinanceFlow.Application.UseCases.Analytics.Queries.GetCashFlow;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Analytics;

public class GetCashFlowQueryHandlerTests
{
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<ICacheService> _cache = new();

    private GetCashFlowQueryHandler CreateHandler() =>
        new(_transactionRepository.Object, _cache.Object);

    private void SetupCache() =>
        _cache.Setup(c => c.GetOrSetAsync(
            It.IsAny<string>(),
            It.IsAny<Func<Task<CashFlowDto>>>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()))
        .Returns<string, Func<Task<CashFlowDto>>, TimeSpan, CancellationToken>(
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
    public async Task Handle_DeveRetornarCashFlow_QuandoAgrupamentoPorMes()
    {
        // Arrange
        SetupCache();
        SetupTransactions([
            new Transaction { Type = TransactionType.Income,  Amount = 5000, Date = new DateTime(2026, 1, 10), Status = TransactionStatus.Paid, Category = new Category() },
            new Transaction { Type = TransactionType.Expense, Amount = 2000, Date = new DateTime(2026, 1, 20), Status = TransactionStatus.Paid, Category = new Category() },
            new Transaction { Type = TransactionType.Income,  Amount = 3000, Date = new DateTime(2026, 2, 10), Status = TransactionStatus.Paid, Category = new Category() },
        ]);

        var query = new GetCashFlowQuery(
            UserId: Guid.NewGuid(),
            From: new DateTime(2026, 1, 1),
            To: new DateTime(2026, 2, 28),
            GroupBy: "month");

        // Act
        var result = await CreateHandler().Handle(query, default);

        // Assert
        result.Should().NotBeNull();
        result.Periods.Should().HaveCount(2);
        result.TotalIncome.Should().Be(8000);
        result.TotalExpenses.Should().Be(2000);
        result.NetBalance.Should().Be(6000);
    }

    [Fact]
    public async Task Handle_DeveRetornarCashFlow_QuandoAgrupamentoPorDia()
    {
        // Arrange
        SetupCache();
        SetupTransactions([
            new Transaction { Type = TransactionType.Expense, Amount = 150, Date = new DateTime(2026, 1, 15), Status = TransactionStatus.Paid, Category = new Category() },
        ]);

        var query = new GetCashFlowQuery(
            UserId: Guid.NewGuid(),
            From: new DateTime(2026, 1, 1),
            To: new DateTime(2026, 1, 7),
            GroupBy: "day");

        // Act
        var result = await CreateHandler().Handle(query, default);

        // Assert
        result.Periods.Should().HaveCount(7);
        result.GroupBy.Should().Be("day");
    }

    [Fact]
    public async Task Handle_DeveExcluirTransacoesAgendadas()
    {
        // Arrange
        SetupCache();
        SetupTransactions([
            new Transaction { Type = TransactionType.Income,  Amount = 5000, Date = new DateTime(2026, 1, 10), Status = TransactionStatus.Paid,      Category = new Category() },
            new Transaction { Type = TransactionType.Income,  Amount = 1000, Date = new DateTime(2026, 1, 15), Status = TransactionStatus.Scheduled, Category = new Category() },
        ]);

        var query = new GetCashFlowQuery(
            UserId: Guid.NewGuid(),
            From: new DateTime(2026, 1, 1),
            To: new DateTime(2026, 1, 31),
            GroupBy: "month");

        // Act
        var result = await CreateHandler().Handle(query, default);

        // Assert — apenas a transação Paid deve ser contabilizada
        result.TotalIncome.Should().Be(5000);
    }

    [Fact]
    public async Task Handle_DeveRetornarZeros_QuandoNaoHaTransacoes()
    {
        // Arrange
        SetupCache();
        SetupTransactions([]);

        var query = new GetCashFlowQuery(
            UserId: Guid.NewGuid(),
            From: new DateTime(2026, 1, 1),
            To: new DateTime(2026, 3, 31),
            GroupBy: "month");

        // Act
        var result = await CreateHandler().Handle(query, default);

        // Assert
        result.TotalIncome.Should().Be(0);
        result.TotalExpenses.Should().Be(0);
        result.NetBalance.Should().Be(0);
    }

    [Fact]
    public async Task Handle_DeveCalcularSaldoCumulativoCorrectamente()
    {
        // Arrange
        SetupCache();
        SetupTransactions([
            new Transaction { Type = TransactionType.Income,  Amount = 5000, Date = new DateTime(2026, 1, 10), Status = TransactionStatus.Paid, Category = new Category() },
            new Transaction { Type = TransactionType.Expense, Amount = 2000, Date = new DateTime(2026, 2, 10), Status = TransactionStatus.Paid, Category = new Category() },
        ]);

        var query = new GetCashFlowQuery(
            UserId: Guid.NewGuid(),
            From: new DateTime(2026, 1, 1),
            To: new DateTime(2026, 2, 28),
            GroupBy: "month");

        // Act
        var result = await CreateHandler().Handle(query, default);

        // Assert
        var periods = result.Periods.ToList();
        periods[0].CumulativeBalance.Should().Be(5000);
        periods[1].CumulativeBalance.Should().Be(3000); // 5000 - 2000
    }
}
