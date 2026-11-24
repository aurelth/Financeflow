using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.DTOs.Imports;
using FinanceFlow.Application.Services;
using FinanceFlow.Application.UseCases.Imports.Commands.ConfirmImport;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Imports;

public class ConfirmImportCommandHandlerTests
{
    private readonly Mock<IBankImportRepository> _bankImportRepository = new();
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<ITransferRepository> _transferRepository = new();
    private readonly Mock<IDashboardCacheInvalidator> _dashboardCacheInvalidator = new();

    private ConfirmImportCommandHandler CreateHandler() =>
        new(_bankImportRepository.Object,
            _transactionRepository.Object,
            _categoryRepository.Object,
            _transferRepository.Object,
            _dashboardCacheInvalidator.Object);

    [Fact]
    public async Task Handle_DeveCriarTransacoes_QuandoSelecionadas()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var importId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var importTransaction = new BankImportTransaction
        {
            Id = Guid.NewGuid(),
            Hash = "hash123",
            Amount = 150.00m,
            Description = "IFOOD",
            Type = TransactionType.Expense,
            Date = DateTime.UtcNow,
            IsDuplicate = false,
            IsSelected = true,
        };

        var bankImport = new BankImport
        {
            Id = importId,
            UserId = userId,
            Status = BankImportStatus.Completed,
            Transactions = [importTransaction],
        };

        var category = new Category
        {
            Id = categoryId,
            Name = "Alimentação",
            Type = TransactionType.Expense,
            IsDefault = true,
        };

        _bankImportRepository
            .Setup(r => r.GetByIdAsync(importId, userId, default))
            .ReturnsAsync(bankImport);

        _categoryRepository
            .Setup(r => r.GetByIdAsync(categoryId, userId, default))
            .ReturnsAsync(category);

        var request = new ConfirmImportRequestDto([
            new ConfirmImportItemDto(importTransaction.Id, true, categoryId, "Expense")
        ]);

        var command = new ConfirmImportCommand(importId, userId, request);

        // Act
        var result = await CreateHandler().Handle(command, default);

        // Assert
        result.Imported.Should().Be(1);
        result.Errors.Should().Be(0);

        _transactionRepository.Verify(r =>
            r.AddAsync(It.Is<Transaction>(t =>
                t.UserId == userId &&
                t.Amount == 150.00m &&
                t.ImportHash == "hash123" &&
                t.CategoryId == categoryId),
            default), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveMarcarImportacaoComoConfirmada_AposImportarLancamentos()
    {
        var userId = Guid.NewGuid();
        var importId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var row = new BankImportTransaction
        {
            Id = Guid.NewGuid(), Hash = "h", Amount = 10m, Description = "IFOOD",
            Type = TransactionType.Expense, Date = DateTime.UtcNow, IsSelected = true,
        };
        var bankImport = new BankImport
        {
            Id = importId, UserId = userId, Status = BankImportStatus.Completed, Transactions = [row],
        };
        _bankImportRepository.Setup(r => r.GetByIdAsync(importId, userId, default)).ReturnsAsync(bankImport);
        _categoryRepository
            .Setup(r => r.GetByIdAsync(categoryId, userId, default))
            .ReturnsAsync(new Category { Id = categoryId, Name = "Alimentação", Type = TransactionType.Expense, IsDefault = true });

        await CreateHandler().Handle(new ConfirmImportCommand(importId, userId,
            new ConfirmImportRequestDto([new ConfirmImportItemDto(row.Id, true, categoryId, "Expense")])), default);

        bankImport.ConfirmedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_NaoDeveMarcarComoConfirmada_QuandoTudoFalhaAoGravar()
    {
        var userId = Guid.NewGuid();
        var importId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var row = new BankImportTransaction
        {
            Id = Guid.NewGuid(), Hash = "h", Amount = 10m, Description = "IFOOD",
            Type = TransactionType.Expense, Date = DateTime.UtcNow, IsSelected = true,
        };
        var bankImport = new BankImport
        {
            Id = importId, UserId = userId, Status = BankImportStatus.Completed, Transactions = [row],
        };
        _bankImportRepository.Setup(r => r.GetByIdAsync(importId, userId, default)).ReturnsAsync(bankImport);
        _categoryRepository
            .Setup(r => r.GetByIdAsync(categoryId, userId, default))
            .ReturnsAsync(new Category { Id = categoryId, Name = "Alimentação", Type = TransactionType.Expense, IsDefault = true });
        _transactionRepository
            .Setup(r => r.AddAsync(It.IsAny<Transaction>(), default))
            .ThrowsAsync(new InvalidOperationException("falha ao gravar"));

        await CreateHandler().Handle(new ConfirmImportCommand(importId, userId,
            new ConfirmImportRequestDto([new ConfirmImportItemDto(row.Id, true, categoryId, "Expense")])), default);

        bankImport.Imported.Should().Be(0);
        bankImport.Errors.Should().Be(1);
        bankImport.ConfirmedAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_DeveIgnorarDuplicatas()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var importId = Guid.NewGuid();

        var importTransaction = new BankImportTransaction
        {
            Id = Guid.NewGuid(),
            Hash = "dupHash",
            Amount = 100.00m,
            Type = TransactionType.Expense,
            Date = DateTime.UtcNow,
            IsDuplicate = true,
            IsSelected = false,
        };

        var bankImport = new BankImport
        {
            Id = importId,
            UserId = userId,
            Status = BankImportStatus.Completed,
            Transactions = [importTransaction],
        };

        _bankImportRepository
            .Setup(r => r.GetByIdAsync(importId, userId, default))
            .ReturnsAsync(bankImport);

        var request = new ConfirmImportRequestDto([
            new ConfirmImportItemDto(importTransaction.Id, true, Guid.NewGuid(), "Expense")
        ]);

        var command = new ConfirmImportCommand(importId, userId, request);

        // Act
        var result = await CreateHandler().Handle(command, default);

        // Assert
        result.Imported.Should().Be(0);
        _transactionRepository.Verify(r =>
            r.AddAsync(It.IsAny<Transaction>(), default), Times.Never);
    }

    // Linha selecionada sem categoria bloqueia a confirmação (nunca some em silêncio)
    [Fact]
    public async Task Handle_DeveBloquearConfirmacao_QuandoCategoryIdEhGuidEmpty()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var importId = Guid.NewGuid();

        var importTransaction = new BankImportTransaction
        {
            Id = Guid.NewGuid(),
            Hash = "hash456",
            Amount = 50.00m,
            Type = TransactionType.Expense,
            Date = DateTime.UtcNow,
            IsDuplicate = false,
            IsSelected = true,
        };

        var bankImport = new BankImport
        {
            Id = importId,
            UserId = userId,
            Status = BankImportStatus.Completed,
            Transactions = [importTransaction],
        };

        _bankImportRepository
            .Setup(r => r.GetByIdAsync(importId, userId, default))
            .ReturnsAsync(bankImport);

        var request = new ConfirmImportRequestDto([
            new ConfirmImportItemDto(importTransaction.Id, true, Guid.Empty, "Expense") // Sem categoria
        ]);

        var command = new ConfirmImportCommand(importId, userId, request);

        // Act
        var act = () => CreateHandler().Handle(command, default);

        // Assert
        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey(importTransaction.Id.ToString());
        ex.Which.Errors[importTransaction.Id.ToString()].Should().Contain(ImportRowErrors.MissingCategory);
        _transactionRepository.Verify(r =>
            r.AddAsync(It.IsAny<Transaction>(), default), Times.Never);
        _bankImportRepository.Verify(r =>
            r.UpdateAsync(It.IsAny<BankImport>(), default), Times.Never);
    }

    // Categoria inexistente bloqueia a confirmação
    [Fact]
    public async Task Handle_DeveBloquearConfirmacao_QuandoCategoriaInexistente()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var importId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var importTransaction = new BankImportTransaction
        {
            Id = Guid.NewGuid(),
            Hash = "hash789",
            Amount = 75.00m,
            Type = TransactionType.Expense,
            Date = DateTime.UtcNow,
            IsDuplicate = false,
            IsSelected = true,
        };

        var bankImport = new BankImport
        {
            Id = importId,
            UserId = userId,
            Status = BankImportStatus.Completed,
            Transactions = [importTransaction],
        };

        _bankImportRepository
            .Setup(r => r.GetByIdAsync(importId, userId, default))
            .ReturnsAsync(bankImport);

        _categoryRepository
            .Setup(r => r.GetByIdAsync(categoryId, userId, default))
            .ReturnsAsync((Category?)null); // Categoria não encontrada

        var request = new ConfirmImportRequestDto([
            new ConfirmImportItemDto(importTransaction.Id, true, categoryId, "Expense")
        ]);

        var command = new ConfirmImportCommand(importId, userId, request);

        // Act
        var act = () => CreateHandler().Handle(command, default);

        // Assert
        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors[importTransaction.Id.ToString()].Should().Contain(ImportRowErrors.InvalidCategory);
        _transactionRepository.Verify(r =>
            r.AddAsync(It.IsAny<Transaction>(), default), Times.Never);
    }

    // Persiste SuggestedCategoryId após confirmação
    [Fact]
    public async Task Handle_DevePersistirSuggestedCategoryId_AposConfirmacao()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var importId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var importTransaction = new BankImportTransaction
        {
            Id = Guid.NewGuid(),
            Hash = "hash999",
            Amount = 200.00m,
            Type = TransactionType.Expense,
            Date = DateTime.UtcNow,
            IsDuplicate = false,
            IsSelected = true,
        };

        var bankImport = new BankImport
        {
            Id = importId,
            UserId = userId,
            Status = BankImportStatus.Completed,
            Transactions = [importTransaction],
        };

        var category = new Category
        {
            Id = categoryId,
            Name = "Transporte",
            Type = TransactionType.Expense,
        };

        _bankImportRepository
            .Setup(r => r.GetByIdAsync(importId, userId, default))
            .ReturnsAsync(bankImport);

        _categoryRepository
            .Setup(r => r.GetByIdAsync(categoryId, userId, default))
            .ReturnsAsync(category);

        var request = new ConfirmImportRequestDto([
            new ConfirmImportItemDto(importTransaction.Id, true, categoryId, "Expense")
        ]);

        var command = new ConfirmImportCommand(importId, userId, request);

        // Act
        await CreateHandler().Handle(command, default);

        // Assert: SuggestedCategoryId deve ser persistido na transação de importação
        importTransaction.SuggestedCategoryId.Should().Be(categoryId);
    }

    // Tipo Transfer enviado pelo frontend deve ser persistido
    [Fact]
    public async Task Handle_DeveUsarTipoTransfer_QuandoEnviadoPeloFrontend()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var importId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var importTransaction = new BankImportTransaction
        {
            Id = Guid.NewGuid(),
            Hash = "hash-transfer",
            Amount = 3000.00m,
            Type = TransactionType.Income, // Original era Income
            Date = DateTime.UtcNow,
            IsDuplicate = false,
            IsSelected = true,
        };

        var bankImport = new BankImport
        {
            Id = importId,
            UserId = userId,
            Status = BankImportStatus.Completed,
            Transactions = [importTransaction],
        };

        var category = new Category
        {
            Id = categoryId,
            Name = "Transferência",
            Type = TransactionType.Transfer,
        };

        _bankImportRepository
            .Setup(r => r.GetByIdAsync(importId, userId, default))
            .ReturnsAsync(bankImport);

        _categoryRepository
            .Setup(r => r.GetByIdAsync(categoryId, userId, default))
            .ReturnsAsync(category);

        var request = new ConfirmImportRequestDto([
            new ConfirmImportItemDto(importTransaction.Id, true, categoryId, "Transfer") // Frontend marcou como Transfer
        ]);

        var command = new ConfirmImportCommand(importId, userId, request);

        // Act
        var result = await CreateHandler().Handle(command, default);

        // Assert
        result.Imported.Should().Be(1);
        _transactionRepository.Verify(r =>
            r.AddAsync(It.Is<Transaction>(t =>
                t.Type == TransactionType.Transfer), // Deve ser Transfer
            default), Times.Once);
    }

    private static BankImportTransaction NewRow(string hash, bool duplicate = false, string externalId = "") => new()
    {
        Id = Guid.NewGuid(),
        Hash = hash,
        ExternalId = externalId,
        Amount = 10m,
        Description = "ITEM",
        Type = TransactionType.Expense,
        Date = DateTime.UtcNow,
        IsDuplicate = duplicate,
        IsSelected = !duplicate,
    };

    private (Guid UserId, Guid ImportId, Guid CategoryId) SetupImport(
        BankImportStatus status, int imported, params BankImportTransaction[] rows)
    {
        var userId = Guid.NewGuid();
        var importId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var bankImport = new BankImport
        {
            Id = importId,
            UserId = userId,
            AccountId = Guid.NewGuid(),
            Status = status,
            Imported = imported,
            Transactions = rows,
        };

        _bankImportRepository
            .Setup(r => r.GetByIdAsync(importId, userId, default))
            .ReturnsAsync(bankImport);

        _categoryRepository
            .Setup(r => r.GetByIdAsync(categoryId, userId, default))
            .ReturnsAsync(new Category { Id = categoryId, Name = "Geral", Type = TransactionType.Expense });

        return (userId, importId, categoryId);
    }

    // Validação vem antes da gravação: uma linha inválida impede que as válidas sejam criadas
    [Fact]
    public async Task Handle_NaoDeveCriarNenhumaTransacao_QuandoAlgumaLinhaEstaSemCategoria()
    {
        var ok = NewRow("ok");
        var semCategoria = NewRow("sem");
        var (userId, importId, categoryId) = SetupImport(BankImportStatus.Completed, 0, ok, semCategoria);

        var request = new ConfirmImportRequestDto([
            new ConfirmImportItemDto(ok.Id, true, categoryId, "Expense"),
            new ConfirmImportItemDto(semCategoria.Id, true, Guid.Empty, "Expense"),
        ]);

        var act = () => CreateHandler().Handle(new ConfirmImportCommand(importId, userId, request), default);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainSingle().Which.Key.Should().Be(semCategoria.Id.ToString());
        _transactionRepository.Verify(r => r.AddAsync(It.IsAny<Transaction>(), default), Times.Never);
    }

    // Linha desmarcada sem categoria não bloqueia: o usuário decidiu não importá-la
    [Fact]
    public async Task Handle_NaoDeveBloquear_QuandoLinhaSemCategoriaEstaDesmarcada()
    {
        var ok = NewRow("ok");
        var desmarcada = NewRow("desmarcada");
        var (userId, importId, categoryId) = SetupImport(BankImportStatus.Completed, 0, ok, desmarcada);

        var request = new ConfirmImportRequestDto([
            new ConfirmImportItemDto(ok.Id, true, categoryId, "Expense"),
            new ConfirmImportItemDto(desmarcada.Id, false, Guid.Empty, "Expense"),
        ]);

        var result = await CreateHandler().Handle(new ConfirmImportCommand(importId, userId, request), default);

        result.Imported.Should().Be(1);
    }

    // Transferência não exige categoria
    [Fact]
    public async Task Handle_DeveUsarCategoriaPadraoDeTransferencia_QuandoTransferSemCategoria()
    {
        var row = NewRow("transf");
        var (userId, importId, _) = SetupImport(BankImportStatus.Completed, 0, row);

        var request = new ConfirmImportRequestDto([
            new ConfirmImportItemDto(row.Id, true, Guid.Empty, "Transfer"),
        ]);

        var result = await CreateHandler().Handle(new ConfirmImportCommand(importId, userId, request), default);

        result.Imported.Should().Be(1);
        _transactionRepository.Verify(r => r.AddAsync(It.Is<Transaction>(t =>
            t.Type == TransactionType.Transfer &&
            t.CategoryId == FinanceFlow.Domain.Constants.WellKnownIds.TransferCategoryId), default), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveImportarDuplicata_QuandoImportarMesmoAssim()
    {
        var dup = NewRow("dup", duplicate: true, externalId: "FIT-1");
        var (userId, importId, categoryId) = SetupImport(BankImportStatus.Completed, 0, dup);

        var request = new ConfirmImportRequestDto([
            new ConfirmImportItemDto(dup.Id, true, categoryId, "Expense", ImportAnyway: true),
        ]);

        var result = await CreateHandler().Handle(new ConfirmImportCommand(importId, userId, request), default);

        result.Imported.Should().Be(1);
        _transactionRepository.Verify(r => r.AddAsync(It.Is<Transaction>(t =>
            t.ImportHash == "dup" && t.ExternalId == "FIT-1"), default), Times.Once);
    }

    [Fact]
    public async Task Handle_NaoDeveImportarDuplicata_QuandoSelecionadaSemImportarMesmoAssim()
    {
        var dup = NewRow("dup", duplicate: true);
        var (userId, importId, categoryId) = SetupImport(BankImportStatus.Completed, 0, dup);

        var request = new ConfirmImportRequestDto([
            new ConfirmImportItemDto(dup.Id, true, categoryId, "Expense"),
        ]);

        var result = await CreateHandler().Handle(new ConfirmImportCommand(importId, userId, request), default);

        result.Imported.Should().Be(0);
        _transactionRepository.Verify(r => r.AddAsync(It.IsAny<Transaction>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveGravarExternalIdNulo_QuandoLinhaNaoTemFitId()
    {
        var row = NewRow("sem-fitid", externalId: "");
        var (userId, importId, categoryId) = SetupImport(BankImportStatus.Completed, 0, row);

        var request = new ConfirmImportRequestDto([
            new ConfirmImportItemDto(row.Id, true, categoryId, "Expense"),
        ]);

        await CreateHandler().Handle(new ConfirmImportCommand(importId, userId, request), default);

        _transactionRepository.Verify(r => r.AddAsync(It.Is<Transaction>(t => t.ExternalId == null), default), Times.Once);
    }

    [Theory]
    [InlineData(BankImportStatus.Pending)]
    [InlineData(BankImportStatus.Processing)]
    public async Task Handle_DeveRecusar_QuandoImportacaoAindaNaoFoiProcessada(BankImportStatus status)
    {
        var row = NewRow("x");
        var (userId, importId, categoryId) = SetupImport(status, 0, row);

        var request = new ConfirmImportRequestDto([
            new ConfirmImportItemDto(row.Id, true, categoryId, "Expense"),
        ]);

        var act = () => CreateHandler().Handle(new ConfirmImportCommand(importId, userId, request), default);

        await act.Should().ThrowAsync<ValidationException>();
        _transactionRepository.Verify(r => r.AddAsync(It.IsAny<Transaction>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveRecusar_QuandoImportacaoJaFoiConfirmada()
    {
        var row = NewRow("x");
        var (userId, importId, categoryId) = SetupImport(BankImportStatus.Completed, 3, row);

        var request = new ConfirmImportRequestDto([
            new ConfirmImportItemDto(row.Id, true, categoryId, "Expense"),
        ]);

        var act = () => CreateHandler().Handle(new ConfirmImportCommand(importId, userId, request), default);

        await act.Should().ThrowAsync<ValidationException>();
        _transactionRepository.Verify(r => r.AddAsync(It.IsAny<Transaction>(), default), Times.Never);
    }
}
