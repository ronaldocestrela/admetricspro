using System.Text.Json;
using Master.Domain.FeatureFlags;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Master.Infrastructure.Persistence;

/// <summary>
/// EF Core entity configuration for <see cref="FeatureFlag"/> mapping to table 'FeatureFlags'.
/// </summary>
public sealed class FeatureFlagEntityTypeConfiguration : IEntityTypeConfiguration<FeatureFlag>
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FeatureFlag> builder)
    {
        builder.ToTable("FeatureFlags");

        builder.HasKey(f => f.Id);

        builder
            .Property(f => f.Key)
            .HasMaxLength(100)
            .IsRequired();

        builder
            .HasIndex(f => f.Key)
            .IsUnique();

        builder
            .Property(f => f.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder
            .Property(f => f.Description)
            .HasMaxLength(1000)
            .IsRequired();

        builder
            .Property(f => f.IsEnabled)
            .IsRequired();

        builder
            .Property(f => f.IsKillSwitch)
            .IsRequired();

        builder
            .Property(f => f.TargetingType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder
            .Property(f => f.RolloutPercentage)
            .IsRequired();

        var tenantIdsComparer = new ValueComparer<IReadOnlyCollection<Guid>>(
            (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList());

        builder
            .Property(f => f.TargetTenantIds)
            .HasField("_targetTenantIds")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasConversion(
                ids => JsonSerializer.Serialize(ids, JsonOptions),
                json => string.IsNullOrWhiteSpace(json)
                    ? new List<Guid>()
                    : JsonSerializer.Deserialize<List<Guid>>(json, JsonOptions) ?? new List<Guid>())
            .HasMaxLength(4000)
            .Metadata.SetValueComparer(tenantIdsComparer);

        builder
            .Property(f => f.KillSwitchActivatedAtUtc);

        builder
            .Property(f => f.KillSwitchReason)
            .HasMaxLength(1000);

        builder
            .Property(f => f.KillSwitchTriggeredBy)
            .HasMaxLength(200);

        builder
            .Property(f => f.CreatedBy)
            .HasMaxLength(200)
            .IsRequired();

        builder
            .Property(f => f.CreatedAtUtc)
            .IsRequired();

        builder
            .Property(f => f.UpdatedAtUtc)
            .IsRequired();

        builder
            .Property(f => f.UpdatedBy)
            .HasMaxLength(200);

        builder.HasIndex(f => f.IsKillSwitch);
        builder.HasIndex(f => new { f.IsKillSwitch, f.IsEnabled });
    }
}
