using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Application.UseCases.Dashboard.Queries.GetExpensesByCategory;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Dashboard;

public class GetExpensesByCategoryQueryHandlerTests
{
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<ICacheService> _cache = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid CategoryId1 = Guid.NewGuid();
    private static readonly Guid CategoryId2 = Guid.NewGuid();

    public GetExpensesByCategoryQueryHandlerTests()
    {
        _cache
            .Setup(c => c.GetOrSetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<IEnumerable<ExpensesByCategoryDto>>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<Task<IEnumerable<ExpensesByCategoryDto>>>, TimeSpan, CancellationToken>(
                (_, factory, _, _) => factory());
    }

    private GetExpensesByCategoryQueryHandler CreateHandler() =>
        new(_transactionRepository.Object, _cache.Object);

    [Fact]
    public async Task Handle_DeveAgruparDespesasPorCategoria()
    {
        // Arrange
        var cat1 = new Category { Id = CategoryId1, Name = "Alimentação", Icon = "🍔", Color = "#f97316" };
        var cat2 = new Category { Id = CategoryId2, Name = "Transporte", Icon = "🚗", Color = "#6366f1" };

        var transactions = new List<Transaction>
        {
            new() { Id = Guid.NewGuid(), UserId = UserId, Type = TransactionType.Expense, Status = TransactionStatus.Paid, Amount = 300, Date = new DateTime(2026, 3, 5),  CategoryId = CategoryId1, Category = cat1, Tags = "[]" },
            new() { Id = Guid.NewGuid(), UserId = UserId, Type = TransactionType.Expense, Status = TransactionStatus.Paid, Amount = 200, Date = new DateTime(2026, 3, 10), CategoryId = CategoryId1, Category = cat1, Tags = "[]" },
            new() { Id = Guid.NewGuid(), UserId = UserId, Type = TransactionType.Expense, Status = TransactionStatus.Paid, Amount = 100, Date = new DateTime(2026, 3, 15), CategoryId = CategoryId2, Category = cat2, Tags = "[]" },
        };

        _transactionRepository
            .Setup(r => r.GetPagedByUserAsync(
                UserId, 1, int.MaxValue,
                It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                null, null, TransactionType.Expense, null, null, null, null, default))
            .ReturnsAsync((transactions, transactions.Count));

        var query = new GetExpensesByCategoryQuery(UserId, Month: 3, Year: 2026);

        // Act
        var result = (await CreateHandler().Handle(query, default)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].CategoryName.Should().Be("Alimentação");
        result[0].Total.Should().Be(500);
        result[0].Percentage.Should().Be(83.33m);
        result[1].CategoryName.Should().Be("Transporte");
        result[1].Total.Should().Be(100);
        result[1].Percentage.Should().Be(16.67m);
    }

    [Fact]
    public async Task Handle_DeveRetornarListaVaziaQuandoNaoHaDespesas()
    {
        // Arrange
        _transactionRepository
            .Setup(r => r.GetPagedByUserAsync(
                UserId, 1, int.MaxValue,
                It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                null, null, TransactionType.Expense, null, null, null, null, default))
            .ReturnsAsync((new List<Transaction>(), 0));

        var query = new GetExpensesByCategoryQuery(UserId, Month: 3, Year: 2026);

        // Act
        var result = (await CreateHandler().Handle(query, default)).ToList();

        // Assert
        result.Should().BeEmpty();
    }
}
