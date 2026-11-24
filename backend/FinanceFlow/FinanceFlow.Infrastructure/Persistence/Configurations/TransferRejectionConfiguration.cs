using FinanceFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceFlow.Infrastructure.Persistence.Configurations;

public class TransferRejectionConfiguration : IEntityTypeConfiguration<TransferRejection>
{
    public void Configure(EntityTypeBuilder<TransferRejection> builder)
    {
        builder.ToTable("TransferRejections");
        builder.HasKey(r => r.Id);

        // Sem FK para Transaction: as transações usam soft delete e a rejeição deve sobreviver a isso
        builder.HasIndex(r => new { r.UserId, r.FirstTransactionId, r.SecondTransactionId })
               .IsUnique();
    }
}
