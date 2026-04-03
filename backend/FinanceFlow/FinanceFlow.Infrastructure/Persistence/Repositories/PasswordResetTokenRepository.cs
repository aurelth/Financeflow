using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FinanceFlow.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FinanceFlow.Infrastructure.Persistence.Repositories;

public class PasswordResetTokenRepository(FinanceFlowDbContext context)
    : IPasswordResetTokenRepository
{
    public async Task<PasswordResetToken?> GetValidTokenAsync(
        string token,
        CancellationToken cancellationToken = default) =>
        await context.PasswordResetTokens
            .Include(p => p.User)
            .Where(p =>
                p.Token == token &&
                p.IsUsed == false &&
                p.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task InvalidateUserTokensAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await context.PasswordResetTokens
            .Where(p => p.UserId == userId && p.IsUsed == false)
            .ExecuteUpdateAsync(
                s => s.SetProperty(p => p.IsUsed, true)
                      .SetProperty(p => p.UpdatedAt, DateTime.UtcNow),
                cancellationToken);
    }

    public async Task AddAsync(
        PasswordResetToken token,
        CancellationToken cancellationToken = default)
    {
        await context.PasswordResetTokens.AddAsync(token, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        PasswordResetToken token,
        CancellationToken cancellationToken = default)
    {
        context.PasswordResetTokens.Update(token);
        await context.SaveChangesAsync(cancellationToken);
    }
}
