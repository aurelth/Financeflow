using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Application.UseCases.Dashboard.Queries.GetWeeklyComparison;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Dashboard;

public class GetWeeklyComparisonQueryHandlerTests
{
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<ICacheService> _cache = new();

    private static readonly Guid UserId = Guid.NewGuid();

    public GetWeeklyComparisonQueryHandlerTests()
    {
        _cache
            .Setup(c => c.GetOrSetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<IEnumerable<WeeklyComparisonDto>>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<Task<IEnumerable<WeeklyComparisonDto>>>, TimeSpan, CancellationToken>(
                (_, factory, _, _) => factory());
    }

    private GetWeeklyComparisonQueryHandler CreateHandler() =>
        new(_transactionRepository.Object, _cache.Object);

    [Fact]
    public async Task Handle_DeveRetornar4Semanas()
    {
        // Arrange
        _transactionRepository
            .Setup(r => r.GetPagedByUserAsync(
                UserId, 1, int.MaxValue,
                It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                null, null, null, null, null, null, null, default))
            .ReturnsAsync((new List<Transaction>(), 0));

        var query = new GetWeeklyComparisonQuery(UserId, Month: 3, Year: 2026);

        // Act
        var result = (await CreateHandler().Handle(query, default)).ToList();

        // Assert
        result.Should().HaveCount(4);
        result[0].Week.Should().Be(1);
        result[1].Week.Should().Be(2);
        result[2].Week.Should().Be(3);
        result[3].Week.Should().Be(4);
    }

    [Fact]
    public async Task Handle_DeveAgruparTransacoesPorSemana()
    {
        // Arrange
        var cat = new Category { Name = "Test", Icon = "💰", Color = "#fff" };

        var transactions = new List<Transaction>
        {
            new() { Id = Guid.NewGuid(), UserId = UserId, Type = TransactionType.Income,  Status = TransactionStatus.Paid, Amount = 1000, Date = new DateTime(2026, 3, 3),  CategoryId = Guid.NewGuid(), Category = cat, Tags = "[]" },
            new() { Id = Guid.NewGuid(), UserId = UserId, Type = TransactionType.Expense, Status = TransactionStatus.Paid, Amount = 200,  Date = new DateTime(2026, 3, 5),  CategoryId = Guid.NewGuid(), Category = cat, Tags = "[]" },
            new() { Id = Guid.NewGuid(), UserId = UserId, Type = TransactionType.Expense, Status = TransactionStatus.Paid, Amount = 500,  Date = new DateTime(2026, 3, 20), CategoryId = Guid.NewGuid(), Category = cat, Tags = "[]" },
        };

        _transactionRepository
            .Setup(r => r.GetPagedByUserAsync(
                UserId, 1, int.MaxValue,
                It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                null, null, null, null, null, null, null, default))
            .ReturnsAsync((transactions, transactions.Count));

        var query = new GetWeeklyComparisonQuery(UserId, Month: 3, Year: 2026);

        // Act
        var result = (await CreateHandler().Handle(query, default)).ToList();

        // Assert
        result[0].Income.Should().Be(1000);
        result[0].Expenses.Should().Be(200);
        result[2].Expenses.Should().Be(500);
    }
}
