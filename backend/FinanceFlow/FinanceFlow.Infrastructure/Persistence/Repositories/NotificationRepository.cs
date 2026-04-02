using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FinanceFlow.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FinanceFlow.Infrastructure.Persistence.Repositories;

public class NotificationRepository(FinanceFlowDbContext context) : INotificationRepository
{
    public async Task<IEnumerable<Notification>> GetByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<Notification?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await context.Notifications
            .Where(n => n.Id == id && n.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<int> GetUnreadCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await context.Notifications
            .CountAsync(n => n.UserId == userId && n.IsRead == false, cancellationToken);

    public async Task AddAsync(
        Notification notification,
        CancellationToken cancellationToken = default)
    {
        await context.Notifications.AddAsync(notification, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        Notification notification,
        CancellationToken cancellationToken = default)
    {
        context.Notifications.Update(notification);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllAsReadAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await context.Notifications
            .Where(n => n.UserId == userId && n.IsRead == false)
            .ExecuteUpdateAsync(
                s => s.SetProperty(n => n.IsRead, true)
                      .SetProperty(n => n.UpdatedAt, DateTime.UtcNow),
                cancellationToken);
    }
}
