using FinanceFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceFlow.Infrastructure.Persistence.Configurations;

public class DismissedRecurrenceConfiguration : IEntityTypeConfiguration<DismissedRecurrence>
{
    public void Configure(EntityTypeBuilder<DismissedRecurrence> builder)
    {
        builder.ToTable("DismissedRecurrences");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Key)
               .IsRequired()
               .HasMaxLength(CategoryRule.MaxKeyLength);

        builder.Property(d => d.Type)
               .IsRequired()
               .HasConversion<string>()
               .HasMaxLength(10);

        // Uma dispensa por usuário, tipo e descrição; também atende a leitura por usuário.
        // Sem FK para User (mesmo padrão de CategoryRule/TransferRejection): evita um segundo caminho em cascata.
        builder.HasIndex(d => new { d.UserId, d.Type, d.Key })
               .IsUnique();
    }
}
