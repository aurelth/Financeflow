using FinanceFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceFlow.Infrastructure.Persistence.Configurations;

public class GoalConfiguration : IEntityTypeConfiguration<Goal>
{
    public void Configure(EntityTypeBuilder<Goal> builder)
    {
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(g => g.Emoji)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(g => g.TargetAmount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(g => g.MonthlyContribution)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(g => g.Deadline)
            .IsRequired();

        builder.HasOne(g => g.User)
            .WithMany()
            .HasForeignKey(g => g.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
