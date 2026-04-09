using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.UseCases.Settings.Commands.UpdateNotificationPreferences;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using Moq;

namespace FinanceFlow.UnitTests.Settings;

public class UpdateNotificationPreferencesCommandHandlerTests
{
    private readonly Mock<INotificationPreferencesRepository> _repository = new();
    private readonly UpdateNotificationPreferencesCommandHandler _handler;

    public UpdateNotificationPreferencesCommandHandlerTests()
    {
        _handler = new UpdateNotificationPreferencesCommandHandler(_repository.Object);
    }

    [Fact]
    public async Task Handle_DeveAtualizarPreferencias_QuandoExistem()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var prefs = new UserNotificationPreferences
        {
            UserId = userId,
            BudgetWarningEnabled = true,
            BudgetCriticalEnabled = true,
            TransactionDueTomorrowEnabled = true,
            TransactionDueIn3DaysEnabled = true,
        };

        _repository.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prefs);

        var command = new UpdateNotificationPreferencesCommand(
            UserId: userId,
            BudgetWarningEnabled: false,
            BudgetCriticalEnabled: true,
            TransactionDueTomorrowEnabled: false,
            TransactionDueIn3DaysEnabled: true
        );

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(prefs.BudgetWarningEnabled);
        Assert.True(prefs.BudgetCriticalEnabled);
        Assert.False(prefs.TransactionDueTomorrowEnabled);
        Assert.True(prefs.TransactionDueIn3DaysEnabled);

        _repository.Verify(
            r => r.UpdateAsync(prefs, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DeveLancarNotFoundException_QuandoNaoExistem()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _repository.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserNotificationPreferences?)null);

        var command = new UpdateNotificationPreferencesCommand(
            UserId: userId,
            BudgetWarningEnabled: false,
            BudgetCriticalEnabled: false,
            TransactionDueTomorrowEnabled: false,
            TransactionDueIn3DaysEnabled: false
        );

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));

        _repository.Verify(
            r => r.UpdateAsync(It.IsAny<UserNotificationPreferences>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
