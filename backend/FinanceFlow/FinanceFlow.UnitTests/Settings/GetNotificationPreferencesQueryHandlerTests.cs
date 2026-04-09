using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.UseCases.Settings.Queries.GetNotificationPreferences;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using Moq;

namespace FinanceFlow.UnitTests.Settings;

public class GetNotificationPreferencesQueryHandlerTests
{
    private readonly Mock<INotificationPreferencesRepository> _repository = new();
    private readonly GetNotificationPreferencesQueryHandler _handler;

    public GetNotificationPreferencesQueryHandlerTests()
    {
        _handler = new GetNotificationPreferencesQueryHandler(_repository.Object);
    }

    [Fact]
    public async Task Handle_DeveRetornarPreferencias_QuandoExistem()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var prefs = new UserNotificationPreferences
        {
            UserId = userId,
            BudgetWarningEnabled = true,
            BudgetCriticalEnabled = false,
            TransactionDueTomorrowEnabled = true,
            TransactionDueIn3DaysEnabled = false,
        };

        _repository.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prefs);

        var query = new GetNotificationPreferencesQuery(userId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.BudgetWarningEnabled);
        Assert.False(result.BudgetCriticalEnabled);
        Assert.True(result.TransactionDueTomorrowEnabled);
        Assert.False(result.TransactionDueIn3DaysEnabled);
    }

    [Fact]
    public async Task Handle_DeveLancarNotFoundException_QuandoNaoExistem()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _repository.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserNotificationPreferences?)null);

        var query = new GetNotificationPreferencesQuery(userId);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(query, CancellationToken.None));
    }
}
