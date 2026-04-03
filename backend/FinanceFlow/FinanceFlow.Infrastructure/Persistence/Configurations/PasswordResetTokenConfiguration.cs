using FinanceFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceFlow.Infrastructure.Persistence.Configurations;

public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("PasswordResetTokens");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Token)
               .IsRequired()
               .HasMaxLength(256);

        builder.Property(p => p.ExpiresAt)
               .IsRequired();

        builder.HasIndex(p => p.Token)
               .IsUnique();

        builder.HasOne(p => p.User)
               .WithMany(u => u.PasswordResetTokens)
               .HasForeignKey(p => p.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
