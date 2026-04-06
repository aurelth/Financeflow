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

        _configuration
            .Setup(c => c["Kafka:Topics:TransactionCreated"])
            .Returns("finance.transactions.created");

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
        // Arrange
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
            .ReturnsAsync(ValidCategory);

        // Act
        var act = async () => await CreateHandler().Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*tipo da transação não coincide*");

        _transactionRepository.Verify(r =>
            r.AddAsync(It.IsAny<Transaction>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveGuardarAttachmentName_QuandoAnexoFornecido()
    {
        // Arrange
        var command = new CreateTransactionCommand(
            UserId: UserId,
            Amount: 75.00m,
            Type: TransactionType.Expense,
            Date: DateTime.UtcNow,
            Description: "Com comprovante",
            Status: TransactionStatus.Paid,
            IsRecurring: false,
            RecurrenceType: RecurrenceType.None,
            CategoryId: CategoryId,
            SubcategoryId: null,
            Tags: [],
            AttachmentStream: new MemoryStream(new byte[] { 0xFF, 0xD8 }),
            AttachmentFileName: "comprovante.jpg",
            AttachmentContentType: "image/jpeg");

        _categoryRepository
            .Setup(r => r.GetByIdAsync(CategoryId, UserId, default))
            .ReturnsAsync(ValidCategory);

        _attachmentService
            .Setup(s => s.SaveAsync(
                It.IsAny<Stream>(),
                "comprovante.jpg",
                "image/jpeg",
                UserId,
                default))
            .ReturnsAsync(("attachments/user/guid.jpg", "comprovante.jpg"));

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
            Tags = "[]",
            AttachmentPath = "attachments/user/guid.jpg",
            AttachmentName = "comprovante.jpg",
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
        result.AttachmentName.Should().Be("comprovante.jpg");
        result.AttachmentPath.Should().Be("attachments/user/guid.jpg");

        _attachmentService.Verify(s =>
            s.SaveAsync(
                It.IsAny<Stream>(),
                "comprovante.jpg",
                "image/jpeg",
                UserId,
                default),
            Times.Once);

        _transactionRepository.Verify(r =>
            r.AddAsync(
                It.Is<Transaction>(t =>
                    t.AttachmentPath == "attachments/user/guid.jpg" &&
                    t.AttachmentName == "comprovante.jpg"),
                default),
            Times.Once);
    }

    // Testes de recorrência

    [Fact]
    public async Task Handle_DeveGerarRecurrenceGroupId_QuandoIsRecurringTrue()
    {
        // Arrange
        var command = new CreateTransactionCommand(
            UserId: UserId,
            Amount: 100.00m,
            Type: TransactionType.Expense,
            Date: new DateTime(2026, 12, 1), // Dezembro — nenhuma cópia gerada
            Description: "Assinatura",
            Status: TransactionStatus.Paid,
            IsRecurring: true,
            RecurrenceType: RecurrenceType.Monthly,
            CategoryId: CategoryId,
            SubcategoryId: null,
            Tags: []);

        _categoryRepository
            .Setup(r => r.GetByIdAsync(CategoryId, UserId, default))
            .ReturnsAsync(ValidCategory);

        Transaction? transactionSalva = null;

        _transactionRepository
            .Setup(r => r.AddAsync(It.IsAny<Transaction>(), default))
            .Callback<Transaction, CancellationToken>((t, _) => transactionSalva ??= t)
            .Returns(Task.CompletedTask);

        _transactionRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), UserId, default))
            .ReturnsAsync(() => transactionSalva!);

        // Act
        await CreateHandler().Handle(command, default);

        // Assert
        transactionSalva.Should().NotBeNull();
        transactionSalva!.RecurrenceGroupId.Should().NotBeNull();
        transactionSalva.RecurrenceGroupId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_NaoDeveGerarRecurrenceGroupId_QuandoIsRecurringFalse()
    {
        // Arrange
        var command = new CreateTransactionCommand(
            UserId: UserId,
            Amount: 100.00m,
            Type: TransactionType.Expense,
            Date: DateTime.UtcNow,
            Description: "Não recorrente",
            Status: TransactionStatus.Paid,
            IsRecurring: false,
            RecurrenceType: RecurrenceType.None,
            CategoryId: CategoryId,
            SubcategoryId: null,
            Tags: []);

        _categoryRepository
            .Setup(r => r.GetByIdAsync(CategoryId, UserId, default))
            .ReturnsAsync(ValidCategory);

        Transaction? transactionSalva = null;

        _transactionRepository
            .Setup(r => r.AddAsync(It.IsAny<Transaction>(), default))
            .Callback<Transaction, CancellationToken>((t, _) => transactionSalva ??= t)
            .Returns(Task.CompletedTask);

        _transactionRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), UserId, default))
            .ReturnsAsync(() => transactionSalva!);

        // Act
        await CreateHandler().Handle(command, default);

        // Assert
        transactionSalva!.RecurrenceGroupId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_DeveGerarCopiasParaMesesRestantes_QuandoIsRecurringTrue()
    {
        // Arrange — criada em Outubro, devem ser geradas cópias para Nov e Dez
        var command = new CreateTransactionCommand(
            UserId: UserId,
            Amount: 50.00m,
            Type: TransactionType.Expense,
            Date: new DateTime(2026, 10, 15),
            Description: "Streaming",
            Status: TransactionStatus.Paid,
            IsRecurring: true,
            RecurrenceType: RecurrenceType.Monthly,
            CategoryId: CategoryId,
            SubcategoryId: null,
            Tags: []);

        _categoryRepository
            .Setup(r => r.GetByIdAsync(CategoryId, UserId, default))
            .ReturnsAsync(ValidCategory);

        var todasTransactions = new List<Transaction>();

        _transactionRepository
            .Setup(r => r.AddAsync(It.IsAny<Transaction>(), default))
            .Callback<Transaction, CancellationToken>((t, _) => todasTransactions.Add(t))
            .Returns(Task.CompletedTask);

        _transactionRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), UserId, default))
            .ReturnsAsync(() => todasTransactions.First());

        // Act
        await CreateHandler().Handle(command, default);

        // Assert — 1 original + 2 cópias (Nov e Dez)
        todasTransactions.Should().HaveCount(3);

        var copias = todasTransactions.Skip(1).ToList();
        copias.Should().OnlyContain(t => t.Status == TransactionStatus.Scheduled);
        copias.Should().OnlyContain(t => t.IsRecurring == true);
        copias.Should().OnlyContain(t => t.RecurrenceGroupId == todasTransactions.First().RecurrenceGroupId);
        copias.Select(t => t.Date.Month).Should().BeEquivalentTo([11, 12]);
    }

    [Fact]
    public async Task Handle_NaoDeveGerarCopias_QuandoCriadaEmDezembro()
    {
        // Arrange — criada em Dezembro, nenhuma cópia deve ser gerada
        var command = new CreateTransactionCommand(
            UserId: UserId,
            Amount: 50.00m,
            Type: TransactionType.Expense,
            Date: new DateTime(2026, 12, 1),
            Description: "Streaming Dez",
            Status: TransactionStatus.Paid,
            IsRecurring: true,
            RecurrenceType: RecurrenceType.Monthly,
            CategoryId: CategoryId,
            SubcategoryId: null,
            Tags: []);

        _categoryRepository
            .Setup(r => r.GetByIdAsync(CategoryId, UserId, default))
            .ReturnsAsync(ValidCategory);

        var todasTransactions = new List<Transaction>();

        _transactionRepository
            .Setup(r => r.AddAsync(It.IsAny<Transaction>(), default))
            .Callback<Transaction, CancellationToken>((t, _) => todasTransactions.Add(t))
            .Returns(Task.CompletedTask);

        _transactionRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), UserId, default))
            .ReturnsAsync(() => todasTransactions.First());

        // Act
        await CreateHandler().Handle(command, default);

        // Assert — apenas a transação original, nenhuma cópia
        todasTransactions.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_DeveAjustarDia_QuandoMesDestinoTemMenosDias()
    {
        // Arrange — dia 31 de Janeiro, cópia de Fevereiro deve ir para dia 28
        var command = new CreateTransactionCommand(
            UserId: UserId,
            Amount: 200.00m,
            Type: TransactionType.Expense,
            Date: new DateTime(2026, 1, 31),
            Description: "Conta dia 31",
            Status: TransactionStatus.Paid,
            IsRecurring: true,
            RecurrenceType: RecurrenceType.Monthly,
            CategoryId: CategoryId,
            SubcategoryId: null,
            Tags: []);

        _categoryRepository
            .Setup(r => r.GetByIdAsync(CategoryId, UserId, default))
            .ReturnsAsync(ValidCategory);

        var todasTransactions = new List<Transaction>();

        _transactionRepository
            .Setup(r => r.AddAsync(It.IsAny<Transaction>(), default))
            .Callback<Transaction, CancellationToken>((t, _) => todasTransactions.Add(t))
            .Returns(Task.CompletedTask);

        _transactionRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), UserId, default))
            .ReturnsAsync(() => todasTransactions.First());

        // Act
        await CreateHandler().Handle(command, default);

        // Assert — cópia de Fevereiro deve ter dia 28
        var copiaFevereiro = todasTransactions.FirstOrDefault(t => t.Date.Month == 2);
        copiaFevereiro.Should().NotBeNull();
        copiaFevereiro!.Date.Day.Should().Be(28);
    }
}
