using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
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
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default) =>
        await context.Users
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public async Task<bool> ExistsByEmailAsync(
        string email,
        CancellationToken cancellationToken = default) =>
        await context.Users           
            .AnyAsync(u => u.Email == email, cancellationToken);

    public async Task<bool> ExistsByCpfAsync(
        string cpf,
        CancellationToken cancellationToken = default) =>
        await context.Users            
            .AnyAsync(u => u.Cpf == cpf, cancellationToken);

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

    public async Task<IEnumerable<Guid>> GetAllIdsAsync(
    CancellationToken cancellationToken = default) =>
    await context.Users
        .IgnoreQueryFilters()
        .Where(u => u.DeletedAt == null)
        .Select(u => u.Id)
        .ToListAsync(cancellationToken);

    // Soft-delete do utilizador
    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var user = await context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user is null) return;

        user.DeletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }

    // Respeita query filter — não retorna utilizadores eliminados
    public async Task<User?> GetActiveByEmailAsync(
        string email,
        CancellationToken cancellationToken = default) =>
        await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public async Task<(IEnumerable<User> Users, int Total)> GetAllPagedAsync(
        int page,
        int pageSize,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        var query = context.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AsQueryable();

        // Filtro de status
        if (isActive.HasValue)
            query = isActive.Value
                ? query.Where(u => u.DeletedAt == null)
                : query.Where(u => u.DeletedAt != null);

        // Filtro de busca por nome ou email
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u =>
                u.Name.ToLower().Contains(term) ||
                u.Email.ToLower().Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderBy(u => u.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (users, total);
    }

    // Conta Admins ativos — usado para validar o último Admin
    public async Task<int> CountActiveAdminsAsync(
        CancellationToken cancellationToken = default) =>
        await context.Users
            .IgnoreQueryFilters()
            .CountAsync(u =>
                u.Role == UserRole.Admin &&
                u.DeletedAt == null,
                cancellationToken);

    // Reativa conta desativada
    public async Task ReactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var user = await context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user is null) return;

        user.DeletedAt = null;
        await context.SaveChangesAsync(cancellationToken);
    }
}
