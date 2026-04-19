using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.DTOs.Reports;
using FinanceFlow.Application.UseCases.Analytics.Queries.GetAnnualSummary;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Analytics;

public class GetAnnualSummaryQueryHandlerTests
{
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<ICacheService> _cache = new();

    private GetAnnualSummaryQueryHandler CreateHandler() =>
        new(_transactionRepository.Object, _cache.Object);

    private void SetupCache() =>
        _cache.Setup(c => c.GetOrSetAsync(
            It.IsAny<string>(),
            It.IsAny<Func<Task<AnnualSummaryDto>>>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()))
        .Returns<string, Func<Task<AnnualSummaryDto>>, TimeSpan, CancellationToken>(
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
    public async Task Handle_DeveRetornar12Meses_ParaQualquerAno()
    {
        // Arrange
        SetupCache();
        SetupTransactions([]);

        var query = new GetAnnualSummaryQuery(Guid.NewGuid(), 2026);

        // Act
        var result = await CreateHandler().Handle(query, default);

        // Assert
        result.Months.Should().HaveCount(12);
        result.Year.Should().Be(2026);
    }

    [Fact]
    public async Task Handle_DeveCalcularTotaisCorrectamente()
    {
        // Arrange
        SetupCache();
        SetupTransactions([
            new Transaction { Type = TransactionType.Income,  Amount = 5000, Date = new DateTime(2026, 1, 10), Status = TransactionStatus.Paid, Category = new Category() },
            new Transaction { Type = TransactionType.Expense, Amount = 2000, Date = new DateTime(2026, 1, 20), Status = TransactionStatus.Paid, Category = new Category() },
            new Transaction { Type = TransactionType.Income,  Amount = 4000, Date = new DateTime(2026, 6, 10), Status = TransactionStatus.Paid, Category = new Category() },
        ]);

        var query = new GetAnnualSummaryQuery(Guid.NewGuid(), 2026);

        // Act
        var result = await CreateHandler().Handle(query, default);

        // Assert
        result.TotalIncome.Should().Be(9000);
        result.TotalExpenses.Should().Be(2000);
        result.NetBalance.Should().Be(7000);
    }

    [Fact]
    public async Task Handle_DeveCalcularMediasMensaisCorrectamente()
    {
        // Arrange
        SetupCache();
        SetupTransactions([
            new Transaction { Type = TransactionType.Income,  Amount = 6000, Date = new DateTime(2026, 1, 10), Status = TransactionStatus.Paid, Category = new Category() },
            new Transaction { Type = TransactionType.Income,  Amount = 6000, Date = new DateTime(2026, 2, 10), Status = TransactionStatus.Paid, Category = new Category() },
        ]);

        var query = new GetAnnualSummaryQuery(Guid.NewGuid(), 2026);

        // Act
        var result = await CreateHandler().Handle(query, default);

        // Assert — 2 meses activos, total 12000
        result.AverageMonthlyIncome.Should().Be(6000);
    }

    [Fact]
    public async Task Handle_DeveExcluirTransacoesAgendadas()
    {
        // Arrange
        SetupCache();
        SetupTransactions([
            new Transaction { Type = TransactionType.Income, Amount = 5000, Date = new DateTime(2026, 3, 10), Status = TransactionStatus.Paid,      Category = new Category() },
            new Transaction { Type = TransactionType.Income, Amount = 1000, Date = new DateTime(2026, 3, 15), Status = TransactionStatus.Scheduled, Category = new Category() },
        ]);

        var query = new GetAnnualSummaryQuery(Guid.NewGuid(), 2026);

        // Act
        var result = await CreateHandler().Handle(query, default);

        // Assert
        result.TotalIncome.Should().Be(5000);
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

        var query = new GetAnnualSummaryQuery(Guid.NewGuid(), 2026);

        // Act
        var result = await CreateHandler().Handle(query, default);

        // Assert
        var months = result.Months.ToList();
        months[0].CumulativeBalance.Should().Be(5000);  // Janeiro
        months[1].CumulativeBalance.Should().Be(3000);  // Fevereiro: 5000 - 2000
    }

    // Transfer não deve contar no resumo anual
    [Fact]
    public async Task Handle_DeveExcluirTransferencias_DoResumoAnual()
    {
        // Arrange
        SetupCache();
        SetupTransactions([
            new Transaction { Type = TransactionType.Income,   Amount = 5000, Date = new DateTime(2026, 1, 10), Status = TransactionStatus.Paid, Category = new Category() },
        new Transaction { Type = TransactionType.Expense,  Amount = 2000, Date = new DateTime(2026, 1, 20), Status = TransactionStatus.Paid, Category = new Category() },
        new Transaction { Type = TransactionType.Transfer, Amount = 1000, Date = new DateTime(2026, 1, 15), Status = TransactionStatus.Paid, Category = new Category() },
    ]);

        var query = new GetAnnualSummaryQuery(Guid.NewGuid(), 2026);

        // Act
        var result = await CreateHandler().Handle(query, default);

        // Assert
        result.TotalIncome.Should().Be(5000);
        result.TotalExpenses.Should().Be(2000);
        result.NetBalance.Should().Be(3000);
    }
}
