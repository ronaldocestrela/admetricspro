using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialPublishing.Domain.Posts;

namespace SocialPublishing.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento EF Core de <see cref="OrganicMediaAsset"/> no schema social.
/// </summary>
public sealed class OrganicMediaAssetEntityTypeConfiguration : IEntityTypeConfiguration<OrganicMediaAsset>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<OrganicMediaAsset> builder)
    {
        builder.ToTable("OrganicMediaAssets");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Width).IsRequired();
        builder.Property(m => m.Height).IsRequired();
        builder.Property(m => m.SortOrder).IsRequired();

        builder.Property<Guid>("OrganicPostId").IsRequired();
        builder.HasIndex("OrganicPostId");
    }
}
