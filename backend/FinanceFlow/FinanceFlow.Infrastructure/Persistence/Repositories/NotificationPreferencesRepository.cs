using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FinanceFlow.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FinanceFlow.Infrastructure.Persistence.Repositories;

public class NotificationPreferencesRepository(FinanceFlowDbContext context)
    : INotificationPreferencesRepository
{
    public async Task<UserNotificationPreferences?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await context.Set<UserNotificationPreferences>()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

    public async Task UpdateAsync(
        UserNotificationPreferences preferences,
        CancellationToken cancellationToken = default)
    {
        context.Set<UserNotificationPreferences>().Update(preferences);
        await context.SaveChangesAsync(cancellationToken);
    }
}
