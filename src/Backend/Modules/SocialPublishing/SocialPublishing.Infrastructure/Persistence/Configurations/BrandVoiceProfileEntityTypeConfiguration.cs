using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialPublishing.Domain.Brand;

namespace SocialPublishing.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento EF Core de <see cref="BrandVoiceProfile"/> no schema social.
/// </summary>
public sealed class BrandVoiceProfileEntityTypeConfiguration : IEntityTypeConfiguration<BrandVoiceProfile>
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BrandVoiceProfile> builder)
    {
        builder.ToTable("BrandVoiceProfiles");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.WorkspaceId).IsRequired();
        builder.HasIndex(p => p.WorkspaceId).IsUnique();

        builder.Property(p => p.Tone).HasMaxLength(500).IsRequired();
        builder.Property(p => p.Persona).HasMaxLength(500).IsRequired();
        builder.Property(p => p.EmojiPolicy)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(p => p.HashtagLimit).IsRequired();
        builder.Property(p => p.LanguageCode).HasMaxLength(16).IsRequired();
        builder.Property(p => p.CreatedAtUtc).IsRequired();

        builder.OwnsOne(p => p.PillarWeights, weights =>
        {
            weights.Property(w => w.Authority).HasColumnName("PillarAuthority").IsRequired();
            weights.Property(w => w.Sale).HasColumnName("PillarSale").IsRequired();
            weights.Property(w => w.Connection).HasColumnName("PillarConnection").IsRequired();
            weights.Property(w => w.SocialProof).HasColumnName("PillarSocialProof").IsRequired();
        });

        builder.OwnsOne(p => p.BusinessProfile, business =>
        {
            business.Property(b => b.BusinessSummary)
                .HasColumnName("BusinessSummary")
                .HasMaxLength(BrandBusinessProfile.MaxBusinessSummaryLength);

            business.Property(b => b.TargetAudience)
                .HasColumnName("TargetAudience")
                .HasMaxLength(BrandBusinessProfile.MaxTargetAudienceLength);

            business.Property(b => b.MainOffer)
                .HasColumnName("MainOffer")
                .HasMaxLength(BrandBusinessProfile.MaxMainOfferLength);

            business.Property(b => b.Differentiators)
                .HasColumnName("Differentiators")
                .HasMaxLength(BrandBusinessProfile.MaxDifferentiatorsLength);
        });

        builder.OwnsOne(p => p.VisualIdentity, visual =>
        {
            visual.Property(v => v.PrimaryColor).HasColumnName("PrimaryColor").HasMaxLength(9);
            visual.Property(v => v.SecondaryColor).HasColumnName("SecondaryColor").HasMaxLength(9);
            visual.Property(v => v.AccentColor).HasColumnName("AccentColor").HasMaxLength(9);
        });

        builder.Property<List<string>>("_mandatoryLinks")
            .HasField("_mandatoryLinks")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("MandatoryLinksJson")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<List<string>>(v, JsonOptions) ?? new List<string>())
            .IsRequired();

        builder.Property<List<string>>("_forbiddenTerms")
            .HasField("_forbiddenTerms")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("ForbiddenTermsJson")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<List<string>>(v, JsonOptions) ?? new List<string>())
            .IsRequired();

        builder.Ignore(p => p.MandatoryLinks);
        builder.Ignore(p => p.ForbiddenTerms);
    }
}
