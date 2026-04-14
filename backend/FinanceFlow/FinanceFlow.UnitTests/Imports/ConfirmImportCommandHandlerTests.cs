using FinanceFlow.Application.DTOs.Imports;
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

    private ConfirmImportCommandHandler CreateHandler() =>
        new(_bankImportRepository.Object,
            _transactionRepository.Object,
            _categoryRepository.Object);

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
            new ConfirmImportItemDto(importTransaction.Id, true, categoryId)
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
            new ConfirmImportItemDto(importTransaction.Id, true, Guid.NewGuid())
        ]);

        var command = new ConfirmImportCommand(importId, userId, request);

        // Act
        var result = await CreateHandler().Handle(command, default);

        // Assert
        result.Imported.Should().Be(0);
        _transactionRepository.Verify(r =>
            r.AddAsync(It.IsAny<Transaction>(), default), Times.Never);
    }
}
