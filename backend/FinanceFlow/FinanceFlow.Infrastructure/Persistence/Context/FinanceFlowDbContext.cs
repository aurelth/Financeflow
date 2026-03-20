using FinanceFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceFlow.Infrastructure.Persistence.Context;

public class FinanceFlowDbContext(DbContextOptions<FinanceFlowDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Subcategory> Subcategories => Set<Subcategory>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplica todas as configurações Fluent API da pasta Configurations
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(FinanceFlowDbContext).Assembly);

        // Filtro global de soft delete para todas as entidades BaseEntity
        modelBuilder.Entity<User>().HasQueryFilter(u => u.DeletedAt == null);
        modelBuilder.Entity<Category>().HasQueryFilter(c => c.DeletedAt == null);
        modelBuilder.Entity<Subcategory>().HasQueryFilter(s => s.DeletedAt == null);
        modelBuilder.Entity<Transaction>().HasQueryFilter(t => t.DeletedAt == null);
        modelBuilder.Entity<Budget>().HasQueryFilter(b => b.DeletedAt == null);
        modelBuilder.Entity<Notification>().HasQueryFilter(n => n.DeletedAt == null);
    }

    public override Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        // Atualiza automaticamente UpdatedAt ao modificar uma entidade
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
