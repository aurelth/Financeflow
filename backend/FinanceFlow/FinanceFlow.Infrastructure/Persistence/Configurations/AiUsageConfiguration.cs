using FinanceFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceFlow.Infrastructure.Persistence.Configurations;

public class AiUsageConfiguration : IEntityTypeConfiguration<AiUsage>
{
    public void Configure(EntityTypeBuilder<AiUsage> builder)
    {
        builder.ToTable("AiUsages");
        builder.HasKey(u => u.Id);

        // Uma linha por usuário e dia: é o que torna o incremento atômico e a inserção concorrente segura.
        // Sem FK para User (como TransferRejection/CategoryRule): o histórico de uso sobrevive a ajustes na conta.
        builder.HasIndex(u => new { u.UserId, u.Day })
               .IsUnique();
    }
}
