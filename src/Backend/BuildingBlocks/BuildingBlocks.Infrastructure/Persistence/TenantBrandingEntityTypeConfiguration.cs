using BuildingBlocks.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// EF Core entity type configuration for operational <see cref="TenantBranding"/>.
/// </summary>
public sealed class TenantBrandingEntityTypeConfiguration : IEntityTypeConfiguration<TenantBranding>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TenantBranding> builder)
    {
        builder.ToTable("TenantBranding");

        builder.HasKey(branding => branding.Id);

        builder.Property(branding => branding.PrimaryColor)
            .HasMaxLength(9)
            .IsRequired();

        builder.Property(branding => branding.SecondaryColor)
            .HasMaxLength(9)
            .IsRequired();

        builder.Property(branding => branding.LightLogoUrl)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(branding => branding.DarkLogoUrl)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(branding => branding.FaviconUrl)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(branding => branding.LightLogoObjectKey)
            .HasMaxLength(512)
            .IsRequired(false);

        builder.Property(branding => branding.DarkLogoObjectKey)
            .HasMaxLength(512)
            .IsRequired(false);

        builder.Property(branding => branding.FaviconObjectKey)
            .HasMaxLength(512)
            .IsRequired(false);

        builder.Property(branding => branding.LightLogoContentType)
            .HasMaxLength(128)
            .IsRequired(false);

        builder.Property(branding => branding.DarkLogoContentType)
            .HasMaxLength(128)
            .IsRequired(false);

        builder.Property(branding => branding.FaviconContentType)
            .HasMaxLength(128)
            .IsRequired(false);

        builder.Property(branding => branding.CreatedAtUtc)
            .IsRequired();

        builder.Property(branding => branding.UpdatedAtUtc)
            .IsRequired(false);
    }
}
