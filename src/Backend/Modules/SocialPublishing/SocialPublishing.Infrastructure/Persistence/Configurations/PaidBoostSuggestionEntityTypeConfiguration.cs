using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialPublishing.Domain.Traction;

namespace SocialPublishing.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento EF Core de <see cref="PaidBoostSuggestion"/>.
/// </summary>
public sealed class PaidBoostSuggestionEntityTypeConfiguration : IEntityTypeConfiguration<PaidBoostSuggestion>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PaidBoostSuggestion> builder)
    {
        builder.ToTable("PaidBoostSuggestions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.WorkspaceId).IsRequired();
        builder.Property(s => s.OrganicPostId).IsRequired();
        builder.Property(s => s.Platform)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(s => s.Impressions).IsRequired();
        builder.Property(s => s.EngagementRate).HasPrecision(18, 6);
        builder.Property(s => s.BaselineMedianEngagement).HasPrecision(18, 6);
        builder.Property(s => s.EngagementMultiple).HasPrecision(18, 6);
        builder.Property(s => s.DetectedAtUtc).IsRequired();

        builder.HasIndex(s => new { s.WorkspaceId, s.OrganicPostId, s.Status })
            .IsUnique()
            .HasFilter("[Status] = 'Open'");
    }
}
