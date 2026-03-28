using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.UseCases.Transactions.Commands.DeleteTransaction;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Transactions;

public class DeleteTransactionCommandHandlerTests
{
    private readonly Mock<ITransactionRepository> _transactionRepository = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid TransactionId = Guid.NewGuid();

    private DeleteTransactionCommandHandler CreateHandler() =>
        new(_transactionRepository.Object);

    [Fact]
    public async Task Handle_DeveDeletarTransacao_QuandoExiste()
    {
        // Arrange
        var command = new DeleteTransactionCommand(TransactionId, UserId);

        var transaction = new Transaction
        {
            Id = TransactionId,
            UserId = UserId,
            Tags = "[]"
        };

        _transactionRepository
            .Setup(r => r.GetByIdAsync(TransactionId, UserId, default))
            .ReturnsAsync(transaction);

        _transactionRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Transaction>(), default))
            .Returns(Task.CompletedTask);

        // Act
        await CreateHandler().Handle(command, default);

        // Assert — verifica soft delete
        _transactionRepository.Verify(r =>
            r.UpdateAsync(
                It.Is<Transaction>(t => t.DeletedAt != null),
                default),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DeveLancarNotFoundException_QuandoTransacaoInexistente()
    {
        // Arrange
        var command = new DeleteTransactionCommand(Guid.NewGuid(), UserId);

        _transactionRepository
            .Setup(r => r.GetByIdAsync(command.Id, UserId, default))
            .ReturnsAsync((Transaction?)null);

        // Act
        var act = async () => await CreateHandler().Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
