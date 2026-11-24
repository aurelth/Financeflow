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
        builder.Property(t => t.AttachmentName).HasMaxLength(255);
        builder.Property(t => t.RecurrenceGroupId).IsRequired(false);

        builder.HasOne(t => t.User)
               .WithMany(u => u.Transactions)
               .HasForeignKey(t => t.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Account)
               .WithMany()
               .HasForeignKey(t => t.AccountId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.Property(t => t.ImportHash).HasMaxLength(64);
        builder.Property(t => t.ExternalId).HasMaxLength(100);
        builder.Property(t => t.TransferDirection);

        builder.HasIndex(t => new { t.AccountId, t.Date });

        // Compras parceladas: busca do grupo (plano de parcelas, cancelamento, propagação)
        builder.HasIndex(t => new { t.InstallmentGroupId, t.InstallmentNumber })
               .HasFilter("[InstallmentGroupId] IS NOT NULL");

        // Índices usados pela deduplicação de importações (por conta)
        builder.HasIndex(t => new { t.AccountId, t.ImportHash })
               .HasFilter("[ImportHash] IS NOT NULL");
        builder.HasIndex(t => new { t.AccountId, t.ExternalId })
               .HasFilter("[ExternalId] IS NOT NULL");

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
