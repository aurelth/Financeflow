using FinanceFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceFlow.Infrastructure.Persistence.Configurations;

public class BankImportConfiguration : IEntityTypeConfiguration<BankImport>
{
    public void Configure(EntityTypeBuilder<BankImport> builder)
    {
        builder.ToTable("BankImports");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.FileName)
               .IsRequired()
               .HasMaxLength(255);

        builder.Property(b => b.Status)
               .IsRequired()
               .HasConversion<string>()
               .HasMaxLength(20);

        builder.Property(b => b.ErrorMessage)
               .HasMaxLength(1000);

        builder.Property(b => b.BankId).HasMaxLength(20);
        builder.Property(b => b.AccountKeyHash).HasMaxLength(64);
        builder.Property(b => b.AccountLast4).HasMaxLength(4);

        builder.HasOne(b => b.Account)
               .WithMany()
               .HasForeignKey(b => b.AccountId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.User)
               .WithMany()
               .HasForeignKey(b => b.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
