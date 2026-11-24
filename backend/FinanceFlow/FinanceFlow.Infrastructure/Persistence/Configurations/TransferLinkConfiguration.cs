using FinanceFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceFlow.Infrastructure.Persistence.Configurations;

public class TransferLinkConfiguration : IEntityTypeConfiguration<TransferLink>
{
    public void Configure(EntityTypeBuilder<TransferLink> builder)
    {
        builder.ToTable("TransferLinks");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Origin)
               .IsRequired()
               .HasConversion<string>()
               .HasMaxLength(20);

        builder.HasOne(l => l.User)
               .WithMany()
               .HasForeignKey(l => l.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        // Cada lançamento participa de no máximo um vínculo (garantido pelo banco)
        builder.HasOne(l => l.OutTransaction)
               .WithOne(t => t.LinkAsOut)
               .HasForeignKey<TransferLink>(l => l.OutTransactionId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.InTransaction)
               .WithOne(t => t.LinkAsIn)
               .HasForeignKey<TransferLink>(l => l.InTransactionId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => l.UserId);
    }
}
