using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.UseCases.Notifications.Commands.MarkNotificationAsRead;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Notifications;

public class MarkNotificationAsReadCommandHandlerTests
{
    private readonly Mock<INotificationRepository> _notificationRepository = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid NotificationId = Guid.NewGuid();

    private MarkNotificationAsReadCommandHandler CreateHandler() =>
        new(_notificationRepository.Object);

    [Fact]
    public async Task Handle_DeveMarcarComoLida_QuandoNotificacaoExiste()
    {
        // Arrange
        var command = new MarkNotificationAsReadCommand(NotificationId, UserId);

        var notification = new Notification
        {
            Id = NotificationId,
            UserId = UserId,
            Type = "BudgetWarning",
            Message = "Orçamento atingiu 80%.",
            IsRead = false
        };

        _notificationRepository
            .Setup(r => r.GetByIdAsync(NotificationId, UserId, default))
            .ReturnsAsync(notification);

        _notificationRepository
            .Setup(r => r.UpdateAsync(notification, default))
            .Returns(Task.CompletedTask);

        // Act
        await CreateHandler().Handle(command, default);

        // Assert
        notification.IsRead.Should().BeTrue();

        _notificationRepository.Verify(r =>
            r.UpdateAsync(notification, default), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveLancarNotFoundException_QuandoNotificacaoNaoExiste()
    {
        // Arrange
        var command = new MarkNotificationAsReadCommand(Guid.NewGuid(), UserId);

        _notificationRepository
            .Setup(r => r.GetByIdAsync(command.Id, UserId, default))
            .ReturnsAsync((Notification?)null);

        // Act
        var act = async () => await CreateHandler().Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();

        _notificationRepository.Verify(r =>
            r.UpdateAsync(It.IsAny<Notification>(), default), Times.Never);
    }
}
