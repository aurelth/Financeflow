using FinanceFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceFlow.Infrastructure.Persistence.Configurations;

public class UserNotificationPreferencesConfiguration
    : IEntityTypeConfiguration<UserNotificationPreferences>
{
    public void Configure(EntityTypeBuilder<UserNotificationPreferences> builder)
    {
        builder.ToTable("UserNotificationPreferences");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.BudgetWarningEnabled)
               .IsRequired()
               .HasDefaultValue(true);

        builder.Property(p => p.BudgetCriticalEnabled)
               .IsRequired()
               .HasDefaultValue(true);

        builder.Property(p => p.TransactionDueTomorrowEnabled)
               .IsRequired()
               .HasDefaultValue(true);

        builder.Property(p => p.TransactionDueIn3DaysEnabled)
               .IsRequired()
               .HasDefaultValue(true);

        // Relação 1-to-1 com User
        builder.HasOne(p => p.User)
               .WithOne(u => u.NotificationPreferences)
               .HasForeignKey<UserNotificationPreferences>(p => p.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
