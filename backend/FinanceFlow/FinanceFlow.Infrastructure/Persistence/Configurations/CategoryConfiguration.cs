using FinanceFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceFlow.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Icon).HasMaxLength(50);
        builder.Property(c => c.Color).HasMaxLength(20).HasDefaultValue("#6366f1");
        builder.Property(c => c.Type).IsRequired();
        builder.HasOne(c => c.User)
               .WithMany(u => u.Categories)
               .HasForeignKey(c => c.UserId)
               .OnDelete(DeleteBehavior.Restrict)
               .IsRequired(false);
    }
}
