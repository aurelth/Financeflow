using FinanceFlow.Application.UseCases.Transactions.Queries.GetDueTransactions;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Transactions;

public class GetDueTransactionsQueryHandlerTests
{
    private readonly Mock<ITransactionRepository> _transactionRepository = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid CategoryId = Guid.NewGuid();

    private GetDueTransactionsQueryHandler CreateHandler() =>
        new(_transactionRepository.Object);

    private static Transaction BuildTransaction(
        bool isRecurring,
        TransactionStatus status,
        RecurrenceType recurrenceType = RecurrenceType.Monthly,
        DateTime? date = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            CategoryId = CategoryId,
            Description = "Aluguel",
            Amount = 1500m,
            Type = TransactionType.Expense,
            Date = date ?? DateTime.UtcNow.AddDays(1),
            IsRecurring = isRecurring,
            RecurrenceType = recurrenceType,
            Status = status,
        };

    [Fact]
    public async Task Handle_DeveRetornarTransacoes_QuandoExistemVencimentos()
    {
        // Arrange
        var targetDate = DateTime.UtcNow.AddDays(1).Date;
        var query = new GetDueTransactionsQuery(targetDate);

        var transactions = new List<Transaction>
        {
            BuildTransaction(isRecurring: false, status: TransactionStatus.Scheduled),
            BuildTransaction(isRecurring: true,  status: TransactionStatus.Pending),
        };

        _transactionRepository
            .Setup(r => r.GetDueTransactionsAsync(targetDate, default))
            .ReturnsAsync(transactions);

        // Act
        var result = await CreateHandler().Handle(query, default);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_DeveRetornarListaVazia_QuandoNaoExistemVencimentos()
    {
        // Arrange
        var targetDate = DateTime.UtcNow.AddDays(1).Date;
        var query = new GetDueTransactionsQuery(targetDate);

        _transactionRepository
            .Setup(r => r.GetDueTransactionsAsync(targetDate, default))
            .ReturnsAsync([]);

        // Act
        var result = await CreateHandler().Handle(query, default);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_DeveMapearCamposCorretamente()
    {
        // Arrange
        var targetDate = DateTime.UtcNow.AddDays(1).Date;
        var query = new GetDueTransactionsQuery(targetDate);
        var transaction = BuildTransaction(
            isRecurring: true,
            status: TransactionStatus.Pending,
            recurrenceType: RecurrenceType.Monthly,
            date: targetDate);

        _transactionRepository
            .Setup(r => r.GetDueTransactionsAsync(targetDate, default))
            .ReturnsAsync([transaction]);

        // Act
        var result = (await CreateHandler().Handle(query, default)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(transaction.Id);
        result[0].UserId.Should().Be(UserId);
        result[0].Description.Should().Be("Aluguel");
        result[0].Amount.Should().Be(1500m);
        result[0].IsRecurring.Should().BeTrue();
        result[0].RecurrenceType.Should().Be("Monthly");
    }
}
