using AutoMapper;
using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.Common.Mappings;
using FinanceFlow.Application.UseCases.Transactions.Commands.UpdateTransaction;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Transactions;

public class UpdateTransactionCommandHandlerTests
{
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly IMapper _mapper;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid CategoryId = Guid.NewGuid();
    private static readonly Guid TransactionId = Guid.NewGuid();

    private static readonly Category ValidCategory = new()
    {
        Id = CategoryId,
        Name = "Jogos Online",
        Icon = "🎮",
        Color = "#6366f1",
        Type = TransactionType.Expense,
        UserId = UserId
    };

    private static readonly Transaction ExistingTransaction = new()
    {
        Id = TransactionId,
        UserId = UserId,
        Amount = 100.00m,
        Type = TransactionType.Expense,
        Date = DateTime.UtcNow,
        Description = "Compra original",
        Status = TransactionStatus.Paid,
        CategoryId = CategoryId,
        Category = ValidCategory,
        Tags = "[]"
    };

    public UpdateTransactionCommandHandlerTests()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<TransactionMappingProfile>());
        _mapper = config.CreateMapper();
    }

    private UpdateTransactionCommandHandler CreateHandler() =>
        new(_transactionRepository.Object,
            _categoryRepository.Object,
            _mapper);

    [Fact]
    public async Task Handle_DeveAtualizarTransacao_QuandoDadosSaoValidos()
    {
        // Arrange
        var command = new UpdateTransactionCommand(
            Id: TransactionId,
            UserId: UserId,
            Amount: 200.00m,
            Type: TransactionType.Expense,
            Date: DateTime.UtcNow,
            Description: "Compra atualizada",
            Status: TransactionStatus.Pending,
            IsRecurring: false,
            RecurrenceType: RecurrenceType.None,
            CategoryId: CategoryId,
            SubcategoryId: null,
            Tags: ["atualizado"]);

        _transactionRepository
            .Setup(r => r.GetByIdAsync(TransactionId, UserId, default))
            .ReturnsAsync(ExistingTransaction);

        _categoryRepository
            .Setup(r => r.GetByIdAsync(CategoryId, UserId, default))
            .ReturnsAsync(ValidCategory);

        var updatedTransaction = new Transaction
        {
            Id = TransactionId,
            UserId = UserId,
            Amount = 200.00m,
            Type = TransactionType.Expense,
            Date = ExistingTransaction.Date,
            Description = "Compra atualizada",
            Status = TransactionStatus.Pending,
            CategoryId = CategoryId,
            Category = ValidCategory,
            Tags = "[]"
        };

        _transactionRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Transaction>(), default))
            .Returns(Task.CompletedTask);

        _transactionRepository
            .SetupSequence(r => r.GetByIdAsync(TransactionId, UserId, default))
            .ReturnsAsync(ExistingTransaction)
            .ReturnsAsync(updatedTransaction);

        // Act
        var result = await CreateHandler().Handle(command, default);

        // Assert
        result.Should().NotBeNull();

        _transactionRepository.Verify(r =>
            r.UpdateAsync(It.IsAny<Transaction>(), default), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveLancarNotFoundException_QuandoTransacaoInexistente()
    {
        // Arrange
        var command = new UpdateTransactionCommand(
            Id: Guid.NewGuid(),
            UserId: UserId,
            Amount: 100.00m,
            Type: TransactionType.Expense,
            Date: DateTime.UtcNow,
            Description: "Teste",
            Status: TransactionStatus.Paid,
            IsRecurring: false,
            RecurrenceType: RecurrenceType.None,
            CategoryId: CategoryId,
            SubcategoryId: null,
            Tags: []);

        _transactionRepository
            .Setup(r => r.GetByIdAsync(command.Id, UserId, default))
            .ReturnsAsync((Transaction?)null);

        // Act
        var act = async () => await CreateHandler().Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
