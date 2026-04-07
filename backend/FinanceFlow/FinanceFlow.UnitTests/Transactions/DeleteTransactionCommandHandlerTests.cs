using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.Common.Interfaces; // Adicionado
using FinanceFlow.Application.UseCases.Transactions.Commands.DeleteTransaction;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Transactions;

public class DeleteTransactionCommandHandlerTests
{
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<ICacheService> _cache = new(); // Adicionado

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid TransactionId = Guid.NewGuid();

    public DeleteTransactionCommandHandlerTests()
    {
        // Adicionado: cache não lança exceção por padrão
        _cache
            .Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private DeleteTransactionCommandHandler CreateHandler() =>
        new(_transactionRepository.Object,
            _cache.Object); // Adicionado

    [Fact]
    public async Task Handle_DeveDeletarTransacao_QuandoExiste()
    {
        // Arrange
        var command = new DeleteTransactionCommand(TransactionId, UserId);

        var transaction = new Transaction
        {
            Id = TransactionId,
            UserId = UserId,
            Date = DateTime.UtcNow,
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

    // Adicionado: testa invalidação do cache ao deletar transação
    [Fact]
    public async Task Handle_DeveInvalidarCacheDashboard_QuandoTransacaoDeletada()
    {
        // Arrange
        var command = new DeleteTransactionCommand(TransactionId, UserId);

        var transaction = new Transaction
        {
            Id = TransactionId,
            UserId = UserId,
            Date = new DateTime(2026, 4, 1),
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

        // Assert
        _cache.Verify(c =>
            c.RemoveAsync(
                It.Is<string>(k => k.Contains($"{UserId}:2026:4")),
                default),
            Times.AtLeastOnce);
    }
}
