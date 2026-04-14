using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.UseCases.Imports.Commands.DeleteImport;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Imports;

public class DeleteImportCommandHandlerTests
{
    private readonly Mock<IBankImportRepository> _bankImportRepository = new();

    private DeleteImportCommandHandler CreateHandler() =>
        new(_bankImportRepository.Object);

    [Fact]
    public async Task Handle_DeveDeletarImportacao_QuandoExiste()
    {
        // Arrange
        var importId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var bankImport = new BankImport
        {
            Id = importId,
            UserId = userId,
            FileName = "extrato.ofx",
            Status = BankImportStatus.Completed,
        };

        _bankImportRepository
            .Setup(r => r.GetByIdAsync(importId, userId, default))
            .ReturnsAsync(bankImport);

        var command = new DeleteImportCommand(importId, userId);

        // Act
        await CreateHandler().Handle(command, default);

        // Assert
        _bankImportRepository.Verify(r =>
            r.DeleteAsync(importId, userId, default),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DeveLancarNotFoundException_QuandoNaoExiste()
    {
        // Arrange
        var importId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _bankImportRepository
            .Setup(r => r.GetByIdAsync(importId, userId, default))
            .ReturnsAsync((BankImport?)null);

        var command = new DeleteImportCommand(importId, userId);

        // Act
        var act = () => CreateHandler().Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();

        _bankImportRepository.Verify(r =>
            r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), default),
            Times.Never);
    }
}
