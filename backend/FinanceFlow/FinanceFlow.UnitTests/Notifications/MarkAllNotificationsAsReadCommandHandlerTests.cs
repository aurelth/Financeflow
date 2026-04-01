using FinanceFlow.Application.UseCases.Notifications.Commands.MarkAllNotificationsAsRead;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Notifications;

public class MarkAllNotificationsAsReadCommandHandlerTests
{
    private readonly Mock<INotificationRepository> _notificationRepository = new();

    private static readonly Guid UserId = Guid.NewGuid();

    private MarkAllNotificationsAsReadCommandHandler CreateHandler() =>
        new(_notificationRepository.Object);

    [Fact]
    public async Task Handle_DeveMarcarTodasComoLidas_QuandoExistemNotificacoes()
    {
        // Arrange
        var command = new MarkAllNotificationsAsReadCommand(UserId);

        _notificationRepository
            .Setup(r => r.MarkAllAsReadAsync(UserId, default))
            .Returns(Task.CompletedTask);

        // Act
        await CreateHandler().Handle(command, default);

        // Assert
        _notificationRepository.Verify(r =>
            r.MarkAllAsReadAsync(UserId, default), Times.Once);
    }
}
