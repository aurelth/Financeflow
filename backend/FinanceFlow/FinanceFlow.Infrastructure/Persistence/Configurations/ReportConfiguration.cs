using FinanceFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceFlow.Infrastructure.Persistence.Configurations;

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToTable("Reports");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Type).IsRequired();
        builder.Property(r => r.Status).IsRequired();
        builder.Property(r => r.Month).IsRequired();
        builder.Property(r => r.Year).IsRequired();
        builder.Property(r => r.FilePath).HasMaxLength(1000);
        builder.Property(r => r.FileName).HasMaxLength(255);
        builder.Property(r => r.CompletedAt).IsRequired(false);
        builder.HasOne(r => r.User)
               .WithMany(u => u.Reports)
               .HasForeignKey(r => r.UserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
