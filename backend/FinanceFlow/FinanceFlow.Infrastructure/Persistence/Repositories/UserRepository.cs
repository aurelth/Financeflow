using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FinanceFlow.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FinanceFlow.Infrastructure.Persistence.Repositories;

public class UserRepository(FinanceFlowDbContext context) : IUserRepository
{
    public async Task<User?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default) =>
        await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public async Task<bool> ExistsByEmailAsync(
        string email,
        CancellationToken cancellationToken = default) =>
        await context.Users
            .AnyAsync(u => u.Email == email, cancellationToken);

    public async Task AddAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        await context.Users.AddAsync(user, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        context.Users.Update(user);
        await context.SaveChangesAsync(cancellationToken);
    }
}
