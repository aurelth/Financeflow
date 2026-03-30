using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Application.UseCases.Dashboard.Queries.GetBalanceEvolution;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Dashboard;

public class GetBalanceEvolutionQueryHandlerTests
{
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<ICacheService> _cache = new();

    private static readonly Guid UserId = Guid.NewGuid();

    public GetBalanceEvolutionQueryHandlerTests()
    {
        _cache
            .Setup(c => c.GetOrSetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<IEnumerable<BalanceEvolutionDto>>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<Task<IEnumerable<BalanceEvolutionDto>>>, TimeSpan, CancellationToken>(
                (_, factory, _, _) => factory());

        // Setup genérico como fallback
        _cache
            .Setup(c => c.GetOrSetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<BalanceEvolutionDto>>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<Task<List<BalanceEvolutionDto>>>, TimeSpan, CancellationToken>(
                (_, factory, _, _) => factory());
    }

    private GetBalanceEvolutionQueryHandler CreateHandler() =>
        new(_transactionRepository.Object, _cache.Object);

    [Fact]
    public async Task Handle_DeveRetornarUmPontoPorDia()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = Guid.NewGuid(), UserId = UserId,
                Type = TransactionType.Income, Status = TransactionStatus.Paid,
                Amount = 1000, Date = new DateTime(2026, 1, 1),
                CategoryId = Guid.NewGuid(),
                Category = new Category { Name = "Test", Icon = "💰", Color = "#fff" },
                Tags = "[]",
            },
            new()
            {
                Id = Guid.NewGuid(), UserId = UserId,
                Type = TransactionType.Expense, Status = TransactionStatus.Paid,
                Amount = 200, Date = new DateTime(2026, 1, 3),
                CategoryId = Guid.NewGuid(),
                Category = new Category { Name = "Test", Icon = "💰", Color = "#fff" },
                Tags = "[]",
            },
        };

        _transactionRepository
            .Setup(r => r.GetPagedByUserAsync(
                UserId, 1, int.MaxValue,
                It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                null, null, null, null, null, null, null, default))
            .ReturnsAsync((transactions, transactions.Count));

        var query = new GetBalanceEvolutionQuery(UserId, Month: 1, Year: 2026);

        // Act
        var result = (await CreateHandler().Handle(query, default)).ToList();

        // Assert
        result.Should().NotBeEmpty();
        result.First().Date.Should().Be("2026-01-01");

        // Dia 1: receita 1000, saldo acumulado 1000
        var day1 = result.First(r => r.Date == "2026-01-01");
        day1.Income.Should().Be(1000);
        day1.Balance.Should().Be(1000);

        // Dia 3: despesa 200, saldo acumulado 800
        var day3 = result.First(r => r.Date == "2026-01-03");
        day3.Expenses.Should().Be(200);
        day3.Balance.Should().Be(800);
    }

    [Fact]
    public async Task Handle_DeveIgnorarTransacoesAgendadas()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = Guid.NewGuid(), UserId = UserId,
                Type = TransactionType.Income, Status = TransactionStatus.Scheduled,
                Amount = 5000, Date = new DateTime(2026, 1, 15),
                CategoryId = Guid.NewGuid(),
                Category = new Category { Name = "Test", Icon = "💰", Color = "#fff" },
                Tags = "[]",
            },
        };

        _transactionRepository
            .Setup(r => r.GetPagedByUserAsync(
                UserId, 1, int.MaxValue,
                It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                null, null, null, null, null, null, null, default))
            .ReturnsAsync((transactions, transactions.Count));

        var query = new GetBalanceEvolutionQuery(UserId, Month: 1, Year: 2026);

        // Act
        var result = (await CreateHandler().Handle(query, default)).ToList();

        // Assert
        result.Should().NotBeEmpty();
        result.All(r => r.Balance == 0).Should().BeTrue();
    }
}
