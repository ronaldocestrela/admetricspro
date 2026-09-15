using BuildingBlocks.Domain.Campaigns;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Configuração de mapeamento EF Core fluente para a entidade <see cref="Ad"/>.
/// </summary>
public sealed class AdEntityTypeConfiguration : IEntityTypeConfiguration<Ad>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Ad> builder)
    {
        builder.ToTable("Ads");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.AdSetId)
            .IsRequired();

        builder.Property(a => a.CampaignId)
            .IsRequired();

        builder.Property(a => a.ConnectedAdAccountId)
            .IsRequired();

        builder.Property(a => a.ExternalAdId)
            .HasMaxLength(150)
            .IsRequired();

        builder.HasIndex(a => new { a.AdSetId, a.ExternalAdId })
            .IsUnique();

        builder.Property(a => a.Name)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(a => a.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(a => a.CreativeType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(a => a.Headline)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(a => a.Body)
            .HasMaxLength(4000)
            .IsRequired(false);

        builder.Property(a => a.DestinationUrl)
            .HasMaxLength(2000)
            .IsRequired(false);

        builder.Property(a => a.PreviewUrl)
            .HasMaxLength(2000)
            .IsRequired(false);

        builder.Property(a => a.CallToAction)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(a => a.LastSyncedAtUtc)
            .IsRequired();

        builder.Property(a => a.CreatedAtUtc)
            .IsRequired();

        builder.Property(a => a.UpdatedAtUtc)
            .IsRequired(false);
    }
}
