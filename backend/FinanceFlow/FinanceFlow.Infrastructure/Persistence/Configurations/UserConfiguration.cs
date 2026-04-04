using FinanceFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceFlow.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Name)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(u => u.Email)
               .IsRequired()
               .HasMaxLength(200);

        builder.HasIndex(u => u.Email)
               .IsUnique();

        builder.Property(u => u.PasswordHash)
               .IsRequired();

        builder.Property(u => u.Cpf)
               .IsRequired()
               .HasMaxLength(14);

        builder.HasIndex(u => u.Cpf)
               .IsUnique()
               .HasFilter("[Cpf] <> ''");

        builder.Property(u => u.Gender)
               .IsRequired()
               .HasConversion<string>()
               .HasMaxLength(10);

        builder.Property(u => u.Currency)
               .HasMaxLength(10)
               .HasDefaultValue("BRL");

        builder.Property(u => u.Timezone)
               .HasMaxLength(50)
               .HasDefaultValue("America/Sao_Paulo");
    }
}
