using FinanceFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceFlow.Infrastructure.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Amount).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(t => t.Type).IsRequired();
        builder.Property(t => t.Date).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(500);
        builder.Property(t => t.Status).IsRequired();
        builder.Property(t => t.Tags).HasMaxLength(500).HasDefaultValue("[]");
        builder.Property(t => t.AttachmentPath).HasMaxLength(1000);
        builder.HasOne(t => t.User)
               .WithMany(u => u.Transactions)
               .HasForeignKey(t => t.UserId)
               .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.Category)
               .WithMany(c => c.Transactions)
               .HasForeignKey(t => t.CategoryId)
               .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.Subcategory)
               .WithMany(s => s.Transactions)
               .HasForeignKey(t => t.SubcategoryId)
               .OnDelete(DeleteBehavior.Restrict)
               .IsRequired(false);
    }
}
