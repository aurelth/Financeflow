using FinanceFlow.Application.UseCases.Imports.Commands.ProcessImport;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Imports;

public class ProcessImportCommandHandlerTests
{
    private readonly Mock<IBankImportRepository> _bankImportRepository = new();

    private ProcessImportCommandHandler CreateHandler() =>
        new(_bankImportRepository.Object);

    [Fact]
    public async Task Handle_DeveMarcarDuplicatas_QuandoHashJaExiste()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var importId = Guid.NewGuid();

        var transaction = new BankImportTransaction
        {
            Id = Guid.NewGuid(),
            Hash = "abc123",
            Type = TransactionType.Expense,
        };

        var bankImport = new BankImport
        {
            Id = importId,
            UserId = userId,
            Status = BankImportStatus.Pending,
            Transactions = [transaction],
        };

        _bankImportRepository
            .Setup(r => r.GetByIdAsync(importId, userId, default))
            .ReturnsAsync(bankImport);

        _bankImportRepository
            .Setup(r => r.HashExistsAsync(userId, "abc123", default))
            .ReturnsAsync(true);

        var command = new ProcessImportCommand(importId, userId);

        // Act
        await CreateHandler().Handle(command, default);

        // Assert
        transaction.IsDuplicate.Should().BeTrue();
        transaction.IsSelected.Should().BeFalse();
        bankImport.Duplicates.Should().Be(1);
        bankImport.Status.Should().Be(BankImportStatus.Completed);

        _bankImportRepository.Verify(r =>
            r.UpdateAsync(bankImport, default), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_NaoDeveMarcarDuplicata_QuandoHashNaoExiste()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var importId = Guid.NewGuid();

        var transaction = new BankImportTransaction
        {
            Id = Guid.NewGuid(),
            Hash = "novo123",
            Type = TransactionType.Expense,
        };

        var bankImport = new BankImport
        {
            Id = importId,
            UserId = userId,
            Status = BankImportStatus.Pending,
            Transactions = [transaction],
        };

        _bankImportRepository
            .Setup(r => r.GetByIdAsync(importId, userId, default))
            .ReturnsAsync(bankImport);

        _bankImportRepository
            .Setup(r => r.HashExistsAsync(userId, "novo123", default))
            .ReturnsAsync(false);

        var command = new ProcessImportCommand(importId, userId);

        // Act
        await CreateHandler().Handle(command, default);

        // Assert
        transaction.IsDuplicate.Should().BeFalse();
        transaction.IsSelected.Should().BeTrue();
        bankImport.Duplicates.Should().Be(0);
        bankImport.Status.Should().Be(BankImportStatus.Completed);
    }

    [Fact]
    public async Task Handle_DeveIgnorar_QuandoImportacaoNaoExiste()
    {
        // Arrange
        var importId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _bankImportRepository
            .Setup(r => r.GetByIdAsync(importId, userId, default))
            .ReturnsAsync((BankImport?)null);

        var command = new ProcessImportCommand(importId, userId);

        // Act
        await CreateHandler().Handle(command, default);

        // Assert
        _bankImportRepository.Verify(r =>
            r.UpdateAsync(It.IsAny<BankImport>(), default), Times.Never);
    }
}
