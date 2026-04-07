using AutoMapper;
using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.Common.Interfaces; // Adicionado
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
    private readonly Mock<ICacheService> _cache = new(); // Adicionado
    private readonly IMapper _mapper;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid CategoryId = Guid.NewGuid();
    private static readonly Guid CategoryId2 = Guid.NewGuid();
    private static readonly Guid TransactionId = Guid.NewGuid();
    private static readonly Guid RecurrenceGroup = Guid.NewGuid();

    private static readonly Category ValidCategory = new()
    {
        Id = CategoryId,
        Name = "Jogos Online",
        Icon = "🎮",
        Color = "#6366f1",
        Type = TransactionType.Expense,
        UserId = UserId
    };

    private static readonly Category ValidCategory2 = new()
    {
        Id = CategoryId2,
        Name = "Streaming",
        Icon = "📺",
        Color = "#f59e0b",
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

    private static Transaction CreateRecurringTransaction() => new()
    {
        Id = TransactionId,
        UserId = UserId,
        Amount = 100.00m,
        Type = TransactionType.Expense,
        Date = new DateTime(2026, 10, 15),
        Description = "Assinatura",
        Status = TransactionStatus.Paid,
        CategoryId = CategoryId,
        Category = ValidCategory,
        IsRecurring = true,
        RecurrenceType = RecurrenceType.Monthly,
        RecurrenceGroupId = RecurrenceGroup,
        Tags = "[]"
    };

    public UpdateTransactionCommandHandlerTests()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<TransactionMappingProfile>());
        _mapper = config.CreateMapper();

        // Adicionado: cache não lança exceção por padrão
        _cache
            .Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private UpdateTransactionCommandHandler CreateHandler() =>
        new(_transactionRepository.Object,
            _categoryRepository.Object,
            _cache.Object, // Adicionado
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

    [Fact]
    public async Task Handle_DeveAtualizarAttachmentName_QuandoFornecido()
    {
        // Arrange
        var command = new UpdateTransactionCommand(
            Id: TransactionId,
            UserId: UserId,
            Amount: 100.00m,
            Type: TransactionType.Expense,
            Date: DateTime.UtcNow,
            Description: "Com comprovante",
            Status: TransactionStatus.Paid,
            IsRecurring: false,
            RecurrenceType: RecurrenceType.None,
            CategoryId: CategoryId,
            SubcategoryId: null,
            Tags: [],
            AttachmentPath: "attachments/user/guid.jpg",
            AttachmentName: "comprovante.jpg");

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
            Amount = 100.00m,
            Type = TransactionType.Expense,
            Date = ExistingTransaction.Date,
            Description = "Com comprovante",
            Status = TransactionStatus.Paid,
            CategoryId = CategoryId,
            Category = ValidCategory,
            Tags = "[]",
            AttachmentPath = "attachments/user/guid.jpg",
            AttachmentName = "comprovante.jpg",
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
        result.AttachmentName.Should().Be("comprovante.jpg");
        result.AttachmentPath.Should().Be("attachments/user/guid.jpg");

        _transactionRepository.Verify(r =>
            r.UpdateAsync(
                It.Is<Transaction>(t =>
                    t.AttachmentPath == "attachments/user/guid.jpg" &&
                    t.AttachmentName == "comprovante.jpg"),
                default),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NaoDeveAlterar_AttachmentName_QuandoNaoFornecido()
    {
        // Arrange
        var transactionWithAttachment = new Transaction
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
            Tags = "[]",
            AttachmentPath = "attachments/user/antigo.jpg",
            AttachmentName = "recibo_antigo.jpg",
        };

        var command = new UpdateTransactionCommand(
            Id: TransactionId,
            UserId: UserId,
            Amount: 150.00m,
            Type: TransactionType.Expense,
            Date: DateTime.UtcNow,
            Description: "Atualizado sem mexer no anexo",
            Status: TransactionStatus.Paid,
            IsRecurring: false,
            RecurrenceType: RecurrenceType.None,
            CategoryId: CategoryId,
            SubcategoryId: null,
            Tags: []);

        _transactionRepository
            .Setup(r => r.GetByIdAsync(TransactionId, UserId, default))
            .ReturnsAsync(transactionWithAttachment);

        _categoryRepository
            .Setup(r => r.GetByIdAsync(CategoryId, UserId, default))
            .ReturnsAsync(ValidCategory);

        _transactionRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Transaction>(), default))
            .Returns(Task.CompletedTask);

        _transactionRepository
            .SetupSequence(r => r.GetByIdAsync(TransactionId, UserId, default))
            .ReturnsAsync(transactionWithAttachment)
            .ReturnsAsync(transactionWithAttachment);

        // Act
        await CreateHandler().Handle(command, default);

        // Assert
        _transactionRepository.Verify(r =>
            r.UpdateAsync(
                It.Is<Transaction>(t =>
                    t.AttachmentPath == "attachments/user/antigo.jpg" &&
                    t.AttachmentName == "recibo_antigo.jpg"),
                default),
            Times.Once);
    }

    // Testes de propagação para recorrentes

    [Fact]
    public async Task Handle_DevePropagar_QuandoAmountAlteradoEPropagateTrue()
    {
        // Arrange
        var futura = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            Amount = 100.00m,
            Type = TransactionType.Expense,
            Date = new DateTime(2026, 11, 15),
            Description = "Assinatura",
            Status = TransactionStatus.Scheduled,
            CategoryId = CategoryId,
            Category = ValidCategory,
            IsRecurring = true,
            RecurrenceType = RecurrenceType.Monthly,
            RecurrenceGroupId = RecurrenceGroup,
            Tags = "[]"
        };

        var command = new UpdateTransactionCommand(
            Id: TransactionId,
            UserId: UserId,
            Amount: 250.00m,
            Type: TransactionType.Expense,
            Date: new DateTime(2026, 10, 15),
            Description: "Assinatura",
            Status: TransactionStatus.Paid,
            IsRecurring: true,
            RecurrenceType: RecurrenceType.Monthly,
            CategoryId: CategoryId,
            SubcategoryId: null,
            Tags: [],
            PropagateToFuture: true);

        _transactionRepository
            .SetupSequence(r => r.GetByIdAsync(TransactionId, UserId, default))
            .ReturnsAsync(CreateRecurringTransaction())
            .ReturnsAsync(CreateRecurringTransaction());

        _categoryRepository
            .Setup(r => r.GetByIdAsync(CategoryId, UserId, default))
            .ReturnsAsync(ValidCategory);

        _transactionRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Transaction>(), default))
            .Returns(Task.CompletedTask);

        _transactionRepository
            .Setup(r => r.GetFutureRecurringAsync(RecurrenceGroup, It.IsAny<DateTime>(), default))
            .ReturnsAsync(new List<Transaction> { futura });

        // Act
        await CreateHandler().Handle(command, default);

        // Assert
        _transactionRepository.Verify(r =>
            r.UpdateAsync(It.IsAny<Transaction>(), default), Times.Exactly(2));

        _transactionRepository.Verify(r =>
            r.UpdateAsync(
                It.Is<Transaction>(t => t.Amount == 250.00m),
                default),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_DevePropagarCategoria_QuandoCategoriaAlteradaEPropagateTrue()
    {
        // Arrange
        var futura = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            Amount = 100.00m,
            Type = TransactionType.Expense,
            Date = new DateTime(2026, 11, 15),
            Description = "Assinatura",
            Status = TransactionStatus.Scheduled,
            CategoryId = CategoryId,
            Category = ValidCategory,
            IsRecurring = true,
            RecurrenceType = RecurrenceType.Monthly,
            RecurrenceGroupId = RecurrenceGroup,
            Tags = "[]"
        };

        var command = new UpdateTransactionCommand(
            Id: TransactionId,
            UserId: UserId,
            Amount: 100.00m,
            Type: TransactionType.Expense,
            Date: new DateTime(2026, 10, 15),
            Description: "Assinatura",
            Status: TransactionStatus.Paid,
            IsRecurring: true,
            RecurrenceType: RecurrenceType.Monthly,
            CategoryId: CategoryId2,
            SubcategoryId: null,
            Tags: [],
            PropagateToFuture: true);

        _transactionRepository
            .SetupSequence(r => r.GetByIdAsync(TransactionId, UserId, default))
            .ReturnsAsync(CreateRecurringTransaction())
            .ReturnsAsync(CreateRecurringTransaction());

        _categoryRepository
            .Setup(r => r.GetByIdAsync(CategoryId2, UserId, default))
            .ReturnsAsync(ValidCategory2);

        _transactionRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Transaction>(), default))
            .Returns(Task.CompletedTask);

        _transactionRepository
            .Setup(r => r.GetFutureRecurringAsync(RecurrenceGroup, It.IsAny<DateTime>(), default))
            .ReturnsAsync(new List<Transaction> { futura });

        // Act
        await CreateHandler().Handle(command, default);

        // Assert
        _transactionRepository.Verify(r =>
            r.UpdateAsync(
                It.Is<Transaction>(t =>
                    t.Id == futura.Id &&
                    t.CategoryId == CategoryId2),
                default),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NaoDevePropagar_QuandoPropagateToFutureFalse()
    {
        // Arrange
        var command = new UpdateTransactionCommand(
            Id: TransactionId,
            UserId: UserId,
            Amount: 250.00m,
            Type: TransactionType.Expense,
            Date: new DateTime(2026, 10, 15),
            Description: "Assinatura",
            Status: TransactionStatus.Paid,
            IsRecurring: true,
            RecurrenceType: RecurrenceType.Monthly,
            CategoryId: CategoryId,
            SubcategoryId: null,
            Tags: [],
            PropagateToFuture: false);

        _transactionRepository
            .SetupSequence(r => r.GetByIdAsync(TransactionId, UserId, default))
            .ReturnsAsync(CreateRecurringTransaction())
            .ReturnsAsync(CreateRecurringTransaction());

        _categoryRepository
            .Setup(r => r.GetByIdAsync(CategoryId, UserId, default))
            .ReturnsAsync(ValidCategory);

        _transactionRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Transaction>(), default))
            .Returns(Task.CompletedTask);

        // Act
        await CreateHandler().Handle(command, default);

        // Assert
        _transactionRepository.Verify(r =>
            r.GetFutureRecurringAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), default),
            Times.Never);

        _transactionRepository.Verify(r =>
            r.UpdateAsync(It.IsAny<Transaction>(), default), Times.Once);
    }

    [Fact]
    public async Task Handle_NaoDevePropagar_QuandoNadaRelevanteFoiAlterado()
    {
        // Arrange
        var command = new UpdateTransactionCommand(
            Id: TransactionId,
            UserId: UserId,
            Amount: 100.00m,
            Type: TransactionType.Expense,
            Date: new DateTime(2026, 10, 15),
            Description: "Assinatura",
            Status: TransactionStatus.Pending,
            IsRecurring: true,
            RecurrenceType: RecurrenceType.Monthly,
            CategoryId: CategoryId,
            SubcategoryId: null,
            Tags: [],
            PropagateToFuture: true);

        _transactionRepository
            .SetupSequence(r => r.GetByIdAsync(TransactionId, UserId, default))
            .ReturnsAsync(CreateRecurringTransaction())
            .ReturnsAsync(CreateRecurringTransaction());

        _categoryRepository
            .Setup(r => r.GetByIdAsync(CategoryId, UserId, default))
            .ReturnsAsync(ValidCategory);

        _transactionRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Transaction>(), default))
            .Returns(Task.CompletedTask);

        // Act
        await CreateHandler().Handle(command, default);

        // Assert
        _transactionRepository.Verify(r =>
            r.GetFutureRecurringAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), default),
            Times.Never);

        _transactionRepository.Verify(r =>
            r.UpdateAsync(It.IsAny<Transaction>(), default), Times.Once);
    }

    // Adicionado: testa invalidação do cache ao atualizar transação
    [Fact]
    public async Task Handle_DeveInvalidarCacheDashboard_QuandoTransacaoAtualizada()
    {
        // Arrange
        var command = new UpdateTransactionCommand(
            Id: TransactionId,
            UserId: UserId,
            Amount: 200.00m,
            Type: TransactionType.Expense,
            Date: DateTime.UtcNow,
            Description: "Atualizado",
            Status: TransactionStatus.Paid,
            IsRecurring: false,
            RecurrenceType: RecurrenceType.None,
            CategoryId: CategoryId,
            SubcategoryId: null,
            Tags: []);

        _transactionRepository
            .SetupSequence(r => r.GetByIdAsync(TransactionId, UserId, default))
            .ReturnsAsync(ExistingTransaction)
            .ReturnsAsync(ExistingTransaction);

        _categoryRepository
            .Setup(r => r.GetByIdAsync(CategoryId, UserId, default))
            .ReturnsAsync(ValidCategory);

        _transactionRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Transaction>(), default))
            .Returns(Task.CompletedTask);

        // Act
        await CreateHandler().Handle(command, default);

        // Assert
        _cache.Verify(c =>
            c.RemoveAsync(It.IsAny<string>(), default),
            Times.AtLeastOnce);
    }
}
