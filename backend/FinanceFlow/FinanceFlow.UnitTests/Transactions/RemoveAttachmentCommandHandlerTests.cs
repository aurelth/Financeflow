using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.UseCases.Transactions.Commands.RemoveAttachment;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Transactions;

public class RemoveAttachmentCommandHandlerTests
{
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<IAttachmentService> _attachmentService = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid TransactionId = Guid.NewGuid();

    private RemoveAttachmentCommandHandler CreateHandler() =>
        new(_transactionRepository.Object, _attachmentService.Object);

    [Fact]
    public async Task Handle_DeveRemoverAnexo_QuandoTransacaoTemAnexo()
    {
        // Arrange
        var command = new RemoveAttachmentCommand(TransactionId, UserId);

        var transaction = new Transaction
        {
            Id = TransactionId,
            UserId = UserId,
            AttachmentPath = "attachments/user/comprovante.pdf",
            Tags = "[]"
        };

        _transactionRepository
            .Setup(r => r.GetByIdAsync(TransactionId, UserId, default))
            .ReturnsAsync(transaction);

        _attachmentService
            .Setup(s => s.DeleteAsync(transaction.AttachmentPath))
            .Returns(Task.CompletedTask);

        _transactionRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Transaction>(), default))
            .Returns(Task.CompletedTask);

        // Act
        await CreateHandler().Handle(command, default);

        // Assert — ficheiro removido do disco
        _attachmentService.Verify(s =>
            s.DeleteAsync("attachments/user/comprovante.pdf"),
            Times.Once);

        // Assert — caminho limpo na base de dados
        _transactionRepository.Verify(r =>
            r.UpdateAsync(
                It.Is<Transaction>(t => t.AttachmentPath == null),
                default),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NaoFazNada_QuandoTransacaoNaoTemAnexo()
    {
        // Arrange
        var command = new RemoveAttachmentCommand(TransactionId, UserId);

        var transaction = new Transaction
        {
            Id = TransactionId,
            UserId = UserId,
            AttachmentPath = null,
            Tags = "[]"
        };

        _transactionRepository
            .Setup(r => r.GetByIdAsync(TransactionId, UserId, default))
            .ReturnsAsync(transaction);

        // Act
        await CreateHandler().Handle(command, default);

        // Assert — não chama DeleteAsync nem UpdateAsync
        _attachmentService.Verify(s =>
            s.DeleteAsync(It.IsAny<string>()),
            Times.Never);

        _transactionRepository.Verify(r =>
            r.UpdateAsync(It.IsAny<Transaction>(), default),
            Times.Never);
    }

    [Fact]
    public async Task Handle_DeveLancarNotFoundException_QuandoTransacaoNaoExiste()
    {
        // Arrange
        var command = new RemoveAttachmentCommand(Guid.NewGuid(), UserId);

        _transactionRepository
            .Setup(r => r.GetByIdAsync(command.Id, UserId, default))
            .ReturnsAsync((Transaction?)null);

        // Act
        var act = async () => await CreateHandler().Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();

        _attachmentService.Verify(s =>
            s.DeleteAsync(It.IsAny<string>()),
            Times.Never);
    }
}
