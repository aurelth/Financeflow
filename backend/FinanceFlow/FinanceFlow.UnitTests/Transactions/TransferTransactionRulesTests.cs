using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.UseCases.Transactions.Commands.CreateTransaction;
using FinanceFlow.Application.UseCases.Transactions.Commands.DeleteTransaction;
using FinanceFlow.Application.UseCases.Transactions.Commands.UpdateTransaction;
using FinanceFlow.Domain.Constants;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;

namespace FinanceFlow.UnitTests.Transactions;

/// <summary>Regras de criar, editar e excluir lançamentos quando eles são pontas de transferência.</summary>
public class TransferTransactionRulesTests
{
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<IAccountRepository> _accountRepository = new();
    private readonly Mock<ITransferRepository> _transferRepository = new();
    private readonly Mock<IEventPublisher> _eventPublisher = new();
    private readonly Mock<IAttachmentService> _attachmentService = new();
    private readonly Mock<IConfiguration> _configuration = new();
    private readonly Mock<ICacheService> _cache = new();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _expenseCategoryId = Guid.NewGuid();

    private Category TransferCategory => new()
    {
        Id = WellKnownIds.TransferCategoryId,
        Name = "Transferência",
        Icon = "↔",
        Color = "#818cf8",
        Type = TransactionType.Transfer,
    };

    private Category ExpenseCategory => new()
    {
        Id = _expenseCategoryId,
        Name = "Mercado",
        Icon = "🛒",
        Color = "#f59e0b",
        Type = TransactionType.Expense,
        UserId = _userId,
    };

    public TransferTransactionRulesTests()
    {

        _configuration.Setup(c => c["Kafka:Topics:TransactionCreated"]).Returns("finance.transactions.created");
        _eventPublisher
            .Setup(e => e.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _cache
            .Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _categoryRepository
            .Setup(r => r.GetByIdAsync(WellKnownIds.TransferCategoryId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => TransferCategory);
        _categoryRepository
            .Setup(r => r.GetByIdAsync(_expenseCategoryId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => ExpenseCategory);

        _accountRepository
            .Setup(r => r.GetByIdAsync(_accountId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Account { Id = _accountId, UserId = _userId, Name = "Conta A" });
    }

    // Criar

    private CreateTransactionCommandHandler CreateHandler() =>
        new(_transactionRepository.Object, _categoryRepository.Object, _accountRepository.Object,
            _eventPublisher.Object, _attachmentService.Object, _configuration.Object,
            _cache.Object);

    private CreateTransactionCommand CreateCommand(
        TransactionType type, Guid categoryId, TransferDirection? direction) => new(
        UserId: _userId,
        Amount: 100m,
        Type: type,
        Date: new DateTime(2026, 2, 10),
        Description: "Teste",
        Status: TransactionStatus.Paid,
        IsRecurring: false,
        RecurrenceType: RecurrenceType.None,
        CategoryId: categoryId,
        SubcategoryId: null,
        Tags: [],
        AccountId: _accountId,
        TransferDirection: direction);

    private void SetupCreatedLookup(Category category) =>
        _transactionRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, Guid _, CancellationToken _) => new Transaction
            {
                Id = id,
                UserId = _userId,
                Category = category,
                CategoryId = category.Id,
                Tags = "[]",
            });

    [Fact]
    public async Task Create_DeveExigirSentido_QuandoTipoEhTransferencia()
    {
        var act = () => CreateHandler().Handle(
            CreateCommand(TransactionType.Transfer, WellKnownIds.TransferCategoryId, direction: null), default);

        await act.Should().ThrowAsync<ValidationException>();
        _transactionRepository.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_DeveGravarOSentido_QuandoTransferencia()
    {
        SetupCreatedLookup(TransferCategory);

        await CreateHandler().Handle(
            CreateCommand(TransactionType.Transfer, WellKnownIds.TransferCategoryId, TransferDirection.In), default);

        _transactionRepository.Verify(r => r.AddAsync(
            It.Is<Transaction>(t => t.Type == TransactionType.Transfer && t.TransferDirection == TransferDirection.In),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_DeveDescartarOSentido_QuandoNaoEhTransferencia()
    {
        SetupCreatedLookup(ExpenseCategory);

        await CreateHandler().Handle(
            CreateCommand(TransactionType.Expense, _expenseCategoryId, TransferDirection.Out), default);

        _transactionRepository.Verify(r => r.AddAsync(
            It.Is<Transaction>(t => t.Type == TransactionType.Expense && t.TransferDirection == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // Editar

    private UpdateTransactionCommandHandler UpdateHandler() =>
        new(_transactionRepository.Object, _categoryRepository.Object, _accountRepository.Object,
            _transferRepository.Object, _cache.Object);

    private Transaction ExistingTransfer(TransferDirection direction = TransferDirection.Out) => new()
    {
        Id = Guid.NewGuid(),
        UserId = _userId,
        AccountId = _accountId,
        Amount = 100m,
        Type = TransactionType.Transfer,
        TransferDirection = direction,
        Date = new DateTime(2026, 2, 10),
        Description = "PIX",
        Status = TransactionStatus.Paid,
        CategoryId = WellKnownIds.TransferCategoryId,
        Category = TransferCategory,
        Tags = "[]",
    };

    // O handler solta a categoria antes de gravar e depois recarrega a transação; o repositório real devolve
    // com o Include. O stub devolve o mesmo objeto, então a categoria correspondente é reanexada aqui.
    private Transaction Recarregada(Transaction transaction)
    {
        transaction.Category = transaction.CategoryId == WellKnownIds.TransferCategoryId
            ? TransferCategory
            : ExpenseCategory;
        return transaction;
    }

    private void SetupExisting(Transaction transaction, bool linked)
    {
        _transactionRepository
            .Setup(r => r.GetByIdAsync(transaction.Id, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => Recarregada(transaction));
        _transferRepository
            .Setup(r => r.IsLinkedAsync(transaction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(linked);
    }

    private UpdateTransactionCommand UpdateCommand(
        Transaction existing,
        decimal? amount = null,
        TransactionType? type = null,
        Guid? categoryId = null,
        TransferDirection? direction = null,
        Guid? accountId = null) => new(
        Id: existing.Id,
        UserId: _userId,
        Amount: amount ?? existing.Amount,
        Type: type ?? existing.Type,
        Date: existing.Date,
        Description: "Descrição nova",
        Status: TransactionStatus.Paid,
        IsRecurring: false,
        RecurrenceType: RecurrenceType.None,
        CategoryId: categoryId ?? existing.CategoryId,
        SubcategoryId: null,
        Tags: [],
        AccountId: accountId,
        TransferDirection: direction);

    [Fact]
    public async Task Update_DeveRecusarMudancaDeValor_QuandoTransferenciaVinculada()
    {
        var tx = ExistingTransfer();
        SetupExisting(tx, linked: true);

        var act = () => UpdateHandler().Handle(UpdateCommand(tx, amount: 150m), default);

        await act.Should().ThrowAsync<ValidationException>().WithMessage("*vinculada*");
        _transactionRepository.Verify(r => r.UpdateAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_DeveRecusarMudancaDeConta_QuandoTransferenciaVinculada()
    {
        var tx = ExistingTransfer();
        SetupExisting(tx, linked: true);

        var act = () => UpdateHandler().Handle(UpdateCommand(tx, accountId: Guid.NewGuid()), default);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Update_DeveRecusarMudancaDeSentido_QuandoTransferenciaVinculada()
    {
        var tx = ExistingTransfer(TransferDirection.Out);
        SetupExisting(tx, linked: true);

        var act = () => UpdateHandler().Handle(UpdateCommand(tx, direction: TransferDirection.In), default);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Update_DevePermitirEditarDescricao_QuandoTransferenciaVinculada_EManterOSentido()
    {
        var tx = ExistingTransfer(TransferDirection.In);
        SetupExisting(tx, linked: true);

        await UpdateHandler().Handle(UpdateCommand(tx), default);

        _transactionRepository.Verify(r => r.UpdateAsync(
            It.Is<Transaction>(t =>
                t.Description == "Descrição nova" &&
                t.Type == TransactionType.Transfer &&
                t.TransferDirection == TransferDirection.In),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_DeveExigirSentido_QuandoLancamentoComumViraTransferencia()
    {
        var expense = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            AccountId = _accountId,
            Amount = 100m,
            Type = TransactionType.Expense,
            Date = new DateTime(2026, 2, 10),
            CategoryId = _expenseCategoryId,
            Category = ExpenseCategory,
            Tags = "[]",
        };
        SetupExisting(expense, linked: false);

        var act = () => UpdateHandler().Handle(
            UpdateCommand(expense, type: TransactionType.Transfer, categoryId: WellKnownIds.TransferCategoryId), default);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Update_DeveGravarOSentido_QuandoLancamentoComumViraTransferencia()
    {
        var expense = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            AccountId = _accountId,
            Amount = 100m,
            Type = TransactionType.Expense,
            Date = new DateTime(2026, 2, 10),
            CategoryId = _expenseCategoryId,
            Category = ExpenseCategory,
            Tags = "[]",
        };
        SetupExisting(expense, linked: false);

        await UpdateHandler().Handle(
            UpdateCommand(expense, type: TransactionType.Transfer,
                categoryId: WellKnownIds.TransferCategoryId, direction: TransferDirection.Out), default);

        _transactionRepository.Verify(r => r.UpdateAsync(
            It.Is<Transaction>(t => t.Type == TransactionType.Transfer && t.TransferDirection == TransferDirection.Out),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_DeveLimparOSentido_QuandoTransferenciaSemParViraDespesa()
    {
        var tx = ExistingTransfer(TransferDirection.Out);
        SetupExisting(tx, linked: false);

        await UpdateHandler().Handle(
            UpdateCommand(tx, type: TransactionType.Expense, categoryId: _expenseCategoryId), default);

        _transactionRepository.Verify(r => r.UpdateAsync(
            It.Is<Transaction>(t => t.Type == TransactionType.Expense && t.TransferDirection == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // Excluir

    [Fact]
    public async Task Delete_DeveDesfazerOVinculo_NoMesmoSaveDaExclusao()
    {
        var tx = ExistingTransfer();
        var link = new TransferLink { Id = Guid.NewGuid(), UserId = _userId, OutTransactionId = tx.Id };

        _transactionRepository
            .Setup(r => r.GetByIdAsync(tx.Id, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tx);
        _transferRepository
            .Setup(r => r.GetLinkByTransactionAsync(tx.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(link);

        await new DeleteTransactionCommandHandler(
                _transactionRepository.Object, _transferRepository.Object, _cache.Object)
            .Handle(new DeleteTransactionCommand(tx.Id, _userId), default);

        _transferRepository.Verify(r => r.RemoveLink(link), Times.Once);
        _transactionRepository.Verify(r => r.UpdateAsync(
            It.Is<Transaction>(t => t.DeletedAt != null), It.IsAny<CancellationToken>()), Times.Once);
        // O vínculo só é gravado junto com a exclusão (UpdateAsync salva), nunca separado
        _transferRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Delete_NaoDeveMexerEmVinculos_QuandoLancamentoNaoEhPareado()
    {
        var tx = ExistingTransfer();
        _transactionRepository
            .Setup(r => r.GetByIdAsync(tx.Id, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tx);

        await new DeleteTransactionCommandHandler(
                _transactionRepository.Object, _transferRepository.Object, _cache.Object)
            .Handle(new DeleteTransactionCommand(tx.Id, _userId), default);

        _transferRepository.Verify(r => r.RemoveLink(It.IsAny<TransferLink>()), Times.Never);
    }
}
