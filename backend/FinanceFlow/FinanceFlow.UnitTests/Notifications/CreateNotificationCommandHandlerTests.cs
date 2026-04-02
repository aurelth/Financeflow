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
}
