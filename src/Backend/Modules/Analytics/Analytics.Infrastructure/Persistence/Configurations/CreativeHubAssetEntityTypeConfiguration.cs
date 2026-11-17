using Analytics.Domain.Creatives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analytics.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento EF Core de <see cref="CreativeHubAsset"/>.
/// </summary>
public sealed class CreativeHubAssetEntityTypeConfiguration : IEntityTypeConfiguration<CreativeHubAsset>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CreativeHubAsset> builder)
    {
        builder.ToTable("CreativeHubAssets");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.MediaFingerprint).HasMaxLength(128).IsRequired();
        builder.Property(a => a.Network).HasMaxLength(64).IsRequired();
        builder.Property(a => a.Permalink).HasMaxLength(2048);
        builder.Property(a => a.Impressions).IsRequired();
        builder.Property(a => a.EngagementRate).HasPrecision(18, 6);
        builder.Property(a => a.BaselineMedianEngagement).HasPrecision(18, 6);
        builder.Property(a => a.EngagementMultiple).HasPrecision(18, 6);
        builder.Property(a => a.DetectedAtUtc).IsRequired();

        builder.HasIndex(a => new { a.WorkspaceId, a.MediaFingerprint }).IsUnique();
    }
}
