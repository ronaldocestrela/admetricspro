using Master.Domain.Integrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Master.Infrastructure.Persistence;

/// <summary>
/// EF Core entity configuration for <see cref="ApiQuotaTracker"/> mapping to table 'ApiQuotaTrackers'.
/// </summary>
public sealed class ApiQuotaTrackerEntityTypeConfiguration : IEntityTypeConfiguration<ApiQuotaTracker>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ApiQuotaTracker> builder)
    {
        builder.ToTable("ApiQuotaTrackers");

        builder.HasKey(t => t.Id);

        builder
            .Property(t => t.Platform)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder
            .HasIndex(t => t.Platform)
            .IsUnique();

        builder
            .Property(t => t.MaxLimit)
            .IsRequired();

        builder
            .Property(t => t.CurrentConsumption)
            .IsRequired();

        builder
            .Property(t => t.WarningThresholdPercentage)
            .IsRequired();

        builder
            .Property(t => t.CriticalThresholdPercentage)
            .IsRequired();

        builder
            .Property(t => t.WindowDuration)
            .IsRequired();

        builder
            .Property(t => t.WindowStartUtc)
            .IsRequired();

        builder
            .Property(t => t.AlertLevel)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder
            .Property(t => t.LastUpdatedUtc)
            .IsRequired();

        builder.Ignore(t => t.UsagePercentage);
        builder.Ignore(t => t.DomainEvents);
    }
}
