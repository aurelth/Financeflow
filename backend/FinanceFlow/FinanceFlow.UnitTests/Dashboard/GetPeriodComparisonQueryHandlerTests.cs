using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Application.UseCases.Dashboard.Queries.GetPeriodComparison;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Dashboard;

public class GetPeriodComparisonQueryHandlerTests
{
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<ICacheService> _cache = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid CategoryId = Guid.NewGuid();

    public GetPeriodComparisonQueryHandlerTests()
    {
        _cache
            .Setup(c => c.GetOrSetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<PeriodComparisonDto>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<Task<PeriodComparisonDto>>, TimeSpan, CancellationToken>(
                (_, factory, _, _) => factory());
    }

    private GetPeriodComparisonQueryHandler CreateHandler() =>
        new(_transactionRepository.Object, _cache.Object);

    private static Transaction MakeTransaction(
        TransactionType type,
        decimal amount,
        DateTime date,
        Guid? categoryId = null) => new()
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            Type = type,
            Status = TransactionStatus.Paid,
            Amount = amount,
            Date = date,
            CategoryId = categoryId ?? CategoryId,
            Category = new Category { Id = categoryId ?? CategoryId, Name = "Alimentação", Icon = "🍔", Color = "#f97316" },
            Tags = "[]",
        };

    [Fact]
    public async Task Handle_DeveRetornarDadosDe2Periodos()
    {
        // Arrange
        var jan = new List<Transaction>
        {
            MakeTransaction(TransactionType.Income,  5000, new DateTime(2026, 1, 5)),
            MakeTransaction(TransactionType.Expense, 2000, new DateTime(2026, 1, 10)),
        };

        var fev = new List<Transaction>
        {
            MakeTransaction(TransactionType.Income,  4500, new DateTime(2026, 2, 5)),
            MakeTransaction(TransactionType.Expense, 2500, new DateTime(2026, 2, 10)),
        };

        _transactionRepository
            .Setup(r => r.GetPagedByUserAsync(
                UserId, 1, int.MaxValue,
                It.Is<DateTime>(d => d.Month == 1), It.IsAny<DateTime>(),
                null, null, null, null, null, null, null, default))
            .ReturnsAsync((jan, jan.Count));

        _transactionRepository
            .Setup(r => r.GetPagedByUserAsync(
                UserId, 1, int.MaxValue,
                It.Is<DateTime>(d => d.Month == 2), It.IsAny<DateTime>(),
                null, null, null, null, null, null, null, default))
            .ReturnsAsync((fev, fev.Count));

        var query = new GetPeriodComparisonQuery(UserId, new[]
        {
            (Month: 1, Year: 2026),
            (Month: 2, Year: 2026),
        });

        // Act
        var result = await CreateHandler().Handle(query, default);

        // Assert
        result.Should().NotBeNull();
        result.Periods.Should().HaveCount(2);

        var periods = result.Periods.ToList();
        periods[0].TotalIncome.Should().Be(5000);
        periods[0].TotalExpenses.Should().Be(2000);
        periods[0].Balance.Should().Be(3000);

        periods[1].TotalIncome.Should().Be(4500);
        periods[1].TotalExpenses.Should().Be(2500);
        periods[1].Balance.Should().Be(2000);
    }

    [Fact]
    public async Task Handle_DeveCalcularVariacaoPercentualEntreperiodos()
    {
        // Arrange
        var jan = new List<Transaction>
        {
            MakeTransaction(TransactionType.Expense, 1000, new DateTime(2026, 1, 10)),
        };

        var fev = new List<Transaction>
        {
            MakeTransaction(TransactionType.Expense, 1500, new DateTime(2026, 2, 10)),
        };

        _transactionRepository
            .Setup(r => r.GetPagedByUserAsync(
                UserId, 1, int.MaxValue,
                It.Is<DateTime>(d => d.Month == 1), It.IsAny<DateTime>(),
                null, null, null, null, null, null, null, default))
            .ReturnsAsync((jan, jan.Count));

        _transactionRepository
            .Setup(r => r.GetPagedByUserAsync(
                UserId, 1, int.MaxValue,
                It.Is<DateTime>(d => d.Month == 2), It.IsAny<DateTime>(),
                null, null, null, null, null, null, null, default))
            .ReturnsAsync((fev, fev.Count));

        var query = new GetPeriodComparisonQuery(UserId, new[]
        {
            (Month: 1, Year: 2026),
            (Month: 2, Year: 2026),
        });

        // Act
        var result = await CreateHandler().Handle(query, default);

        // Assert
        var category = result.CategoryComparisons.First();
        category.Values[0].Should().Be(1000);
        category.Values[1].Should().Be(1500);
        category.Variations[0].Should().BeNull();
        category.Variations[1].Should().Be(50.00m); // +50%
    }

    [Fact]
    public async Task Handle_DeveLimitarA3Periodos()
    {
        // Arrange
        _transactionRepository
            .Setup(r => r.GetPagedByUserAsync(
                UserId, 1, int.MaxValue,
                It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                null, null, null, null, null, null, null, default))
            .ReturnsAsync((new List<Transaction>(), 0));

        var query = new GetPeriodComparisonQuery(UserId, new[]
        {
            (Month: 1, Year: 2026),
            (Month: 2, Year: 2026),
            (Month: 3, Year: 2026),
            (Month: 4, Year: 2026), // deve ser ignorado
        });

        // Act
        var result = await CreateHandler().Handle(query, default);

        // Assert
        result.Periods.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_DeveRetornarListaVaziaQuandoNaoHaDespesas()
    {
        // Arrange
        _transactionRepository
            .Setup(r => r.GetPagedByUserAsync(
                UserId, 1, int.MaxValue,
                It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                null, null, null, null, null, null, null, default))
            .ReturnsAsync((new List<Transaction>(), 0));

        var query = new GetPeriodComparisonQuery(UserId, new[]
        {
            (Month: 1, Year: 2026),
            (Month: 2, Year: 2026),
        });

        // Act
        var result = await CreateHandler().Handle(query, default);

        // Assert
        result.CategoryComparisons.Should().BeEmpty();
        result.Periods.All(p => p.TotalExpenses == 0).Should().BeTrue();
    }
}
