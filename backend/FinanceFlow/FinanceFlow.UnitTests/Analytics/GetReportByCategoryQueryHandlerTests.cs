using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.DTOs.Reports;
using FinanceFlow.Application.UseCases.Analytics.Queries.GetReportByCategory;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Analytics;

public class GetReportByCategoryQueryHandlerTests
{
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<ICacheService> _cache = new();

    private GetReportByCategoryQueryHandler CreateHandler() =>
        new(_transactionRepository.Object, _cache.Object);

    private void SetupCache() =>
        _cache.Setup(c => c.GetOrSetAsync(
            It.IsAny<string>(),
            It.IsAny<Func<Task<ReportByCategoryDto>>>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()))
        .Returns<string, Func<Task<ReportByCategoryDto>>, TimeSpan, CancellationToken>(
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

    private static Category MakeCategory(string name, string icon = "🏠", string color = "#fff") =>
        new() { Id = Guid.NewGuid(), Name = name, Icon = icon, Color = color, Type = TransactionType.Expense };

    [Fact]
    public async Task Handle_DeveAgruparPorCategoria()
    {
        // Arrange
        SetupCache();
        var cat = MakeCategory("Alimentação");

        SetupTransactions([
            new Transaction { Type = TransactionType.Expense, Amount = 100, Date = new DateTime(2026, 1, 10), Status = TransactionStatus.Paid, CategoryId = cat.Id, Category = cat },
            new Transaction { Type = TransactionType.Expense, Amount = 200, Date = new DateTime(2026, 1, 20), Status = TransactionStatus.Paid, CategoryId = cat.Id, Category = cat },
        ]);

        var query = new GetReportByCategoryQuery(
            Guid.NewGuid(),
            new DateTime(2026, 1, 1),
            new DateTime(2026, 1, 31),
            TransactionType.Expense);

        // Act
        var result = await CreateHandler().Handle(query, default);

        // Assert
        result.Categories.Should().HaveCount(1);
        result.Categories.First().Amount.Should().Be(300);
        result.Categories.First().TransactionCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_DeveCalcularPercentagemCorrectamente()
    {
        // Arrange
        SetupCache();
        var cat1 = MakeCategory("Alimentação");
        var cat2 = MakeCategory("Transporte");

        SetupTransactions([
            new Transaction { Type = TransactionType.Expense, Amount = 300, Date = new DateTime(2026, 1, 10), Status = TransactionStatus.Paid, CategoryId = cat1.Id, Category = cat1 },
            new Transaction { Type = TransactionType.Expense, Amount = 700, Date = new DateTime(2026, 1, 20), Status = TransactionStatus.Paid, CategoryId = cat2.Id, Category = cat2 },
        ]);

        var query = new GetReportByCategoryQuery(
            Guid.NewGuid(),
            new DateTime(2026, 1, 1),
            new DateTime(2026, 1, 31),
            TransactionType.Expense);

        // Act
        var result = await CreateHandler().Handle(query, default);

        // Assert
        result.TotalExpenses.Should().Be(1000);
        var alimentacao = result.Categories.First(c => c.CategoryName == "Alimentação");
        alimentacao.Percentage.Should().Be(30);
    }

    [Fact]
    public async Task Handle_DeveRetornarListaVazia_QuandoNaoHaTransacoes()
    {
        // Arrange
        SetupCache();
        SetupTransactions([]);

        var query = new GetReportByCategoryQuery(
            Guid.NewGuid(),
            new DateTime(2026, 1, 1),
            new DateTime(2026, 1, 31),
            null);

        // Act
        var result = await CreateHandler().Handle(query, default);

        // Assert
        result.Categories.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_DeveExcluirTransacoesAgendadas()
    {
        // Arrange
        SetupCache();
        var cat = MakeCategory("Alimentação");

        SetupTransactions([
            new Transaction { Type = TransactionType.Expense, Amount = 100, Date = new DateTime(2026, 1, 10), Status = TransactionStatus.Paid,      CategoryId = cat.Id, Category = cat },
            new Transaction { Type = TransactionType.Expense, Amount = 500, Date = new DateTime(2026, 1, 15), Status = TransactionStatus.Scheduled, CategoryId = cat.Id, Category = cat },
        ]);

        var query = new GetReportByCategoryQuery(
            Guid.NewGuid(),
            new DateTime(2026, 1, 1),
            new DateTime(2026, 1, 31),
            TransactionType.Expense);

        // Act
        var result = await CreateHandler().Handle(query, default);

        // Assert
        result.TotalExpenses.Should().Be(100);
    }
}
