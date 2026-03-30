using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.UseCases.Dashboard.Queries.GetDashboardSummary;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Dashboard;

public class GetDashboardSummaryQueryHandlerTests
{
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<ICacheService> _cache = new();

    private static readonly Guid UserId = Guid.NewGuid();

    public GetDashboardSummaryQueryHandlerTests()
    {
        // Cache sempre executa a factory diretamente
        _cache
            .Setup(c => c.GetOrSetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<FinanceFlow.Application.DTOs.DashboardSummaryDto>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<Task<FinanceFlow.Application.DTOs.DashboardSummaryDto>>, TimeSpan, CancellationToken>(
                (_, factory, _, _) => factory());
    }

    private GetDashboardSummaryQueryHandler CreateHandler() =>
        new(_transactionRepository.Object, _cache.Object);

    private static Transaction MakeTransaction(
        TransactionType type,
        TransactionStatus status,
        decimal amount,
        DateTime date) => new()
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            Type = type,
            Status = status,
            Amount = amount,
            Date = date,
            CategoryId = Guid.NewGuid(),
            Category = new Category { Name = "Test", Icon = "💰", Color = "#fff" },
            Tags = "[]",
        };

    [Fact]
    public async Task Handle_DeveCalcularTotaisCorretamente()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            MakeTransaction(TransactionType.Income,  TransactionStatus.Paid,    5000, new DateTime(2026, 3, 5)),
            MakeTransaction(TransactionType.Expense, TransactionStatus.Paid,    2000, new DateTime(2026, 3, 10)),
            MakeTransaction(TransactionType.Expense, TransactionStatus.Pending, 500,  new DateTime(2026, 3, 15)),
        };

        _transactionRepository
            .Setup(r => r.GetPagedByUserAsync(
                UserId, 1, int.MaxValue,
                It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                null, null, null, null, null, null, null, default))
            .ReturnsAsync((transactions, transactions.Count));

        var query = new GetDashboardSummaryQuery(UserId, Month: 3, Year: 2026);

        // Act
        var result = await CreateHandler().Handle(query, default);

        // Assert
        result.TotalIncome.Should().Be(5000);
        result.TotalExpenses.Should().Be(2500);
        result.Balance.Should().Be(2500);
        result.Month.Should().Be(3);
        result.Year.Should().Be(2026);
    }

    [Fact]
    public async Task Handle_DeveCalcularSaldoProjetadoComTransacoesAgendadas()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            MakeTransaction(TransactionType.Income,  TransactionStatus.Paid,      5000, new DateTime(2026, 3, 5)),
            MakeTransaction(TransactionType.Expense, TransactionStatus.Paid,      2000, new DateTime(2026, 3, 10)),
            MakeTransaction(TransactionType.Income,  TransactionStatus.Scheduled, 1000, new DateTime(2026, 3, 25)),
            MakeTransaction(TransactionType.Expense, TransactionStatus.Scheduled, 500,  new DateTime(2026, 3, 28)),
        };

        _transactionRepository
            .Setup(r => r.GetPagedByUserAsync(
                UserId, 1, int.MaxValue,
                It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                null, null, null, null, null, null, null, default))
            .ReturnsAsync((transactions, transactions.Count));

        var query = new GetDashboardSummaryQuery(UserId, Month: 3, Year: 2026);

        // Act
        var result = await CreateHandler().Handle(query, default);

        // Assert
        result.Balance.Should().Be(3000);           // 5000 - 2000
        result.ProjectedBalance.Should().Be(3500);  // 3000 + 1000 - 500
    }

    [Fact]
    public async Task Handle_DeveRetornarZerosQuandoNaoHaTransacoes()
    {
        // Arrange
        _transactionRepository
            .Setup(r => r.GetPagedByUserAsync(
                UserId, 1, int.MaxValue,
                It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                null, null, null, null, null, null, null, default))
            .ReturnsAsync((new List<Transaction>(), 0));

        var query = new GetDashboardSummaryQuery(UserId, Month: 3, Year: 2026);

        // Act
        var result = await CreateHandler().Handle(query, default);

        // Assert
        result.TotalIncome.Should().Be(0);
        result.TotalExpenses.Should().Be(0);
        result.Balance.Should().Be(0);
        result.ProjectedBalance.Should().Be(0);
    }
}
