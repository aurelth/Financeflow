using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Domain.Interfaces;

public interface INotificationPreferencesRepository
{
    Task<UserNotificationPreferences?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        UserNotificationPreferences preferences,
        CancellationToken cancellationToken = default);

    Task CreateAsync(
        UserNotificationPreferences preferences,
        CancellationToken cancellationToken = default);
}
