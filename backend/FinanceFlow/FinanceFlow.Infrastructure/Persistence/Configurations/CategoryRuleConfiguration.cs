using FinanceFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceFlow.Infrastructure.Persistence.Configurations;

public class CategoryRuleConfiguration : IEntityTypeConfiguration<CategoryRule>
{
    public void Configure(EntityTypeBuilder<CategoryRule> builder)
    {
        builder.ToTable("CategoryRules");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Key)
               .IsRequired()
               .HasMaxLength(CategoryRule.MaxKeyLength);

        builder.Property(r => r.Type)
               .IsRequired()
               .HasConversion<string>()
               .HasMaxLength(10);

        // Uma regra por usuário, tipo e descrição: a última escolha do usuário atualiza a existente
        builder.HasIndex(r => new { r.UserId, r.Type, r.Key })
               .IsUnique();

        // Sem FK para User (como TransferRejection): a regra pertence ao usuário só por UserId, e um
        // segundo caminho em cascata User → Category → Rule faria o SQL Server recusar a migração.
        // A categoria excluída fisicamente leva suas regras junto.
        builder.HasOne(r => r.Category)
               .WithMany()
               .HasForeignKey(r => r.CategoryId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
