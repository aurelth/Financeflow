using AutoMapper;
using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.Common.Mappings;
using FinanceFlow.Application.UseCases.Transactions.Commands.CreateTransaction;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;

namespace FinanceFlow.UnitTests.Transactions;

public class CreateTransactionCommandHandlerTests
{
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<IEventPublisher> _eventPublisher = new();
    private readonly Mock<IAttachmentService> _attachmentService = new();
    private readonly Mock<IConfiguration> _configuration = new();
    private readonly IMapper _mapper;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid CategoryId = Guid.NewGuid();

    private static readonly Category ValidCategory = new()
    {
        Id = CategoryId,
        Name = "Jogos Online",
        Icon = "🎮",
        Color = "#6366f1",
        Type = TransactionType.Expense,
        UserId = UserId
    };

    public CreateTransactionCommandHandlerTests()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<TransactionMappingProfile>());
        _mapper = config.CreateMapper();

        // Configuração do tópico Kafka
        _configuration
            .Setup(c => c["Kafka:Topics:TransactionCreated"])
            .Returns("finance.transactions.created");

        // Publisher não lança exceção por padrão
        _eventPublisher
            .Setup(e => e.PublishAsync(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private CreateTransactionCommandHandler CreateHandler() =>
        new(_transactionRepository.Object,
            _categoryRepository.Object,
            _eventPublisher.Object,
            _attachmentService.Object,
            _configuration.Object,
            _mapper);

    [Fact]
    public async Task Handle_DeveCriarTransacao_QuandoDadosSaoValidos()
    {
        // Arrange
        var command = new CreateTransactionCommand(
            UserId: UserId,
            Amount: 100.00m,
            Type: TransactionType.Expense,
            Date: DateTime.UtcNow,
            Description: "Compra de jogo",
            Status: TransactionStatus.Paid,
            IsRecurring: false,
            RecurrenceType: RecurrenceType.None,
            CategoryId: CategoryId,
            SubcategoryId: null,
            Tags: ["jogos"]);

        _categoryRepository
            .Setup(r => r.GetByIdAsync(CategoryId, UserId, default))
            .ReturnsAsync(ValidCategory);

        var createdTransaction = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            Amount = command.Amount,
            Type = command.Type,
            Date = command.Date,
            Description = command.Description,
            Status = command.Status,
            CategoryId = CategoryId,
            Category = ValidCategory,
            Tags = "[]"
        };

        _transactionRepository
            .Setup(r => r.AddAsync(It.IsAny<Transaction>(), default))
            .Returns(Task.CompletedTask);

        _transactionRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), UserId, default))
            .ReturnsAsync(createdTransaction);

        // Act
        var result = await CreateHandler().Handle(command, default);

        // Assert
        result.Should().NotBeNull();
        result.Amount.Should().Be(command.Amount);
        result.CategoryId.Should().Be(CategoryId);

        _transactionRepository.Verify(r =>
            r.AddAsync(It.IsAny<Transaction>(), default), Times.Once);

        _eventPublisher.Verify(e =>
            e.PublishAsync(
                "finance.transactions.created",
                It.IsAny<object>(),
                default),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DeveLancarNotFoundException_QuandoCategoriaInexistente()
    {
        // Arrange
        var command = new CreateTransactionCommand(
            UserId: UserId,
            Amount: 50.00m,
            Type: TransactionType.Expense,
            Date: DateTime.UtcNow,
            Description: "Teste",
            Status: TransactionStatus.Paid,
            IsRecurring: false,
            RecurrenceType: RecurrenceType.None,
            CategoryId: Guid.NewGuid(),
            SubcategoryId: null,
            Tags: []);

        _categoryRepository
            .Setup(r => r.GetByIdAsync(command.CategoryId, UserId, default))
            .ReturnsAsync((Category?)null);

        // Act
        var act = async () => await CreateHandler().Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();

        _transactionRepository.Verify(r =>
            r.AddAsync(It.IsAny<Transaction>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveLancarValidationException_QuandoTipoNaoCoincideComCategoria()
    {
        // Arrange — categoria é Expense mas a transação é Income
        var command = new CreateTransactionCommand(
            UserId: UserId,
            Amount: 200.00m,
            Type: TransactionType.Income,
            Date: DateTime.UtcNow,
            Description: "Teste tipo errado",
            Status: TransactionStatus.Paid,
            IsRecurring: false,
            RecurrenceType: RecurrenceType.None,
            CategoryId: CategoryId,
            SubcategoryId: null,
            Tags: []);

        _categoryRepository
            .Setup(r => r.GetByIdAsync(CategoryId, UserId, default))
            .ReturnsAsync(ValidCategory); // ValidCategory.Type = Expense

        // Act
        var act = async () => await CreateHandler().Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*tipo da transação não coincide*");

        _transactionRepository.Verify(r =>
            r.AddAsync(It.IsAny<Transaction>(), default), Times.Never);
    }
}
