using FinanceFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceFlow.Infrastructure.Persistence.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Type).IsRequired();
        builder.Property(a => a.BankId).HasMaxLength(20);
        builder.Property(a => a.BankName).HasMaxLength(100);
        builder.Property(a => a.AccountKeyHash).HasMaxLength(64);
        builder.Property(a => a.AccountLast4).HasMaxLength(4);
        builder.Property(a => a.InitialBalance).HasColumnType("decimal(18,2)");
        builder.Property(a => a.Currency).IsRequired().HasMaxLength(3).HasDefaultValue("BRL");
        builder.Property(a => a.Color).IsRequired().HasMaxLength(9);
        builder.Property(a => a.Icon).IsRequired().HasMaxLength(50);

        builder.HasOne(a => a.User)
               .WithMany(u => u.Accounts)
               .HasForeignKey(a => a.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        // Reconhecimento da conta pelo OFX: no máximo uma conta por (usuário, banco, chave)
        builder.HasIndex(a => new { a.UserId, a.BankId, a.AccountKeyHash })
               .IsUnique()
               .HasFilter("[AccountKeyHash] IS NOT NULL AND [DeletedAt] IS NULL");

        // No máximo uma "Conta principal" por usuário (também protege contra corrida na criação lazy)
        builder.HasIndex(a => a.UserId)
               .IsUnique()
               .HasDatabaseName("IX_Accounts_UserId_Default")
               .HasFilter("[IsDefault] = 1 AND [DeletedAt] IS NULL");
    }
}
