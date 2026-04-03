using FinanceFlow.Application.UseCases.Notifications.Commands.CreateNotification;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Notifications;

public class CreateNotificationCommandHandlerTests
{
    private readonly Mock<INotificationRepository> _notificationRepository = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ReferenceId = Guid.NewGuid();

    private CreateNotificationCommandHandler CreateHandler() =>
        new(_notificationRepository.Object);

    [Fact]
    public async Task Handle_DeveCriarNotificacao_QuandoDadosSaoValidos()
    {
        // Arrange
        var command = new CreateNotificationCommand(
            UserId: UserId,
            Type: "BudgetWarning",
            Message: "Orçamento atingiu 80%.");

        _notificationRepository
            .Setup(r => r.AddAsync(It.IsAny<Notification>(), default))
            .Returns(Task.CompletedTask);

        // Act
        var result = await CreateHandler().Handle(command, default);

        // Assert
        result.Should().NotBe(Guid.Empty);

        _notificationRepository.Verify(r =>
            r.AddAsync(It.IsAny<Notification>(), default), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveCriarNotificacao_ComIsReadFalse()
    {
        // Arrange
        var command = new CreateNotificationCommand(
            UserId: UserId,
            Type: "BudgetCritical",
            Message: "Orçamento atingiu 100%.");

        Notification? captured = null;

        _notificationRepository
            .Setup(r => r.AddAsync(It.IsAny<Notification>(), default))
            .Callback<Notification, CancellationToken>((n, _) => captured = n)
            .Returns(Task.CompletedTask);

        // Act
        await CreateHandler().Handle(command, default);

        // Assert
        captured.Should().NotBeNull();
        captured!.IsRead.Should().BeFalse();
        captured.UserId.Should().Be(UserId);
        captured.Type.Should().Be("BudgetCritical");
    }

    [Fact]
    public async Task Handle_DeveCriarNotificacao_ComReferenceId() // adicionado
    {
        // Arrange
        var command = new CreateNotificationCommand(
            UserId: UserId,
            Type: "TransactionDueTomorrow",
            Message: "Transação vence amanhã.",
            ReferenceId: ReferenceId);

        Notification? captured = null;

        _notificationRepository
            .Setup(r => r.ExistsForTodayAsync(UserId, "TransactionDueTomorrow", ReferenceId, default))
            .ReturnsAsync(false);

        _notificationRepository
            .Setup(r => r.AddAsync(It.IsAny<Notification>(), default))
            .Callback<Notification, CancellationToken>((n, _) => captured = n)
            .Returns(Task.CompletedTask);

        // Act
        var result = await CreateHandler().Handle(command, default);

        // Assert
        result.Should().NotBe(Guid.Empty);
        captured!.ReferenceId.Should().Be(ReferenceId);

        _notificationRepository.Verify(r =>
            r.AddAsync(It.IsAny<Notification>(), default), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveIgnorarDuplicata_QuandoNotificacaoJaExisteHoje() // adicionado
    {
        // Arrange
        var command = new CreateNotificationCommand(
            UserId: UserId,
            Type: "TransactionDueTomorrow",
            Message: "Transação vence amanhã.",
            ReferenceId: ReferenceId);

        _notificationRepository
            .Setup(r => r.ExistsForTodayAsync(UserId, "TransactionDueTomorrow", ReferenceId, default))
            .ReturnsAsync(true);

        // Act
        var result = await CreateHandler().Handle(command, default);

        // Assert
        result.Should().Be(Guid.Empty);

        _notificationRepository.Verify(r =>
            r.AddAsync(It.IsAny<Notification>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_NaoVerificaDuplicata_QuandoReferenceIdNulo() // adicionado
    {
        // Arrange
        var command = new CreateNotificationCommand(
            UserId: UserId,
            Type: "BudgetWarning",
            Message: "Orçamento atingiu 80%.");

        _notificationRepository
            .Setup(r => r.AddAsync(It.IsAny<Notification>(), default))
            .Returns(Task.CompletedTask);

        // Act
        await CreateHandler().Handle(command, default);

        // Assert — ExistsForTodayAsync nunca deve ser chamado sem ReferenceId
        _notificationRepository.Verify(r =>
            r.ExistsForTodayAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), default),
            Times.Never);
    }
}
