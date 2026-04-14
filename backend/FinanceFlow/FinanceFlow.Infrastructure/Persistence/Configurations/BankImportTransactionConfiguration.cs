using FinanceFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceFlow.Infrastructure.Persistence.Configurations;

public class BankImportTransactionConfiguration : IEntityTypeConfiguration<BankImportTransaction>
{
    public void Configure(EntityTypeBuilder<BankImportTransaction> builder)
    {
        builder.ToTable("BankImportTransactions");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.ExternalId)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(t => t.Description)
               .IsRequired()
               .HasMaxLength(500);

        builder.Property(t => t.Amount)
               .HasPrecision(18, 2);

        builder.Property(t => t.Hash)
               .IsRequired()
               .HasMaxLength(64);

        builder.Property(t => t.Type)
               .IsRequired()
               .HasConversion<string>()
               .HasMaxLength(10);

        // Índice no hash para deduplicação eficiente
        builder.HasIndex(t => new { t.BankImportId, t.Hash });

        builder.HasOne(t => t.BankImport)
               .WithMany(b => b.Transactions)
               .HasForeignKey(t => t.BankImportId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.SuggestedCategory)
               .WithMany()
               .HasForeignKey(t => t.SuggestedCategoryId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.Transaction)
               .WithMany()
               .HasForeignKey(t => t.TransactionId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
