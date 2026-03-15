using FinanceFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceFlow.Infrastructure.Persistence.Configurations;

public class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.ToTable("Budgets");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.LimitAmount).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(b => b.Month).IsRequired();
        builder.Property(b => b.Year).IsRequired();
        builder.HasIndex(b => new { b.UserId, b.CategoryId, b.Month, b.Year }).IsUnique();
        builder.HasOne(b => b.User)
               .WithMany(u => u.Budgets)
               .HasForeignKey(b => b.UserId)
               .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(b => b.Category)
               .WithMany(c => c.Budgets)
               .HasForeignKey(b => b.CategoryId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
