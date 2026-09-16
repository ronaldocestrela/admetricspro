using BuildingBlocks.Domain.Campaigns;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Configuração de mapeamento EF Core fluente para a entidade <see cref="CampaignMetric"/>,
/// estabelecendo chaves primárias, precisões decimais e o índice composto de unicidade para proteção de idempotência.
/// </summary>
public sealed class CampaignMetricEntityTypeConfiguration : IEntityTypeConfiguration<CampaignMetric>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CampaignMetric> builder)
    {
        builder.ToTable("CampaignMetrics");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.WorkspaceId)
            .IsRequired();

        builder.HasIndex(m => m.WorkspaceId);

        builder.Property(m => m.ConnectedAdAccountId)
            .IsRequired();

        builder.Property(m => m.CampaignId)
            .IsRequired();

        builder.Property(m => m.AdSetId)
            .IsRequired(false);

        builder.Property(m => m.AdId)
            .IsRequired(false);

        builder.Property(m => m.Platform)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(m => m.ExternalCampaignId)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(m => m.ExternalAdSetId)
            .HasMaxLength(150)
            .IsRequired(false);

        builder.Property(m => m.ExternalAdId)
            .HasMaxLength(150)
            .IsRequired(false);

        builder.Property(m => m.Date)
            .IsRequired();

        builder.Property(m => m.Hour)
            .IsRequired(false);

        builder.Property(m => m.Granularity)
            .HasConversion<int>()
            .IsRequired();

        // Índice de Idempotência: Garante unicidade por conta, entidade externa, data, hora e granularidade
        builder.HasIndex(m => new
        {
            m.ConnectedAdAccountId,
            m.ExternalCampaignId,
            m.ExternalAdSetId,
            m.ExternalAdId,
            m.Date,
            m.Hour,
            m.Granularity
        }).IsUnique();

        builder.Property(m => m.Spend)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(m => m.Currency)
            .HasMaxLength(10)
            .HasDefaultValue("BRL")
            .IsRequired();

        builder.Property(m => m.Impressions)
            .IsRequired();

        builder.Property(m => m.Clicks)
            .IsRequired();

        builder.Property(m => m.Conversions)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(m => m.ConversionValue)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(m => m.SyncedAtUtc)
            .IsRequired();

        builder.Property(m => m.CreatedAtUtc)
            .IsRequired();

        builder.Property(m => m.UpdatedAtUtc)
            .IsRequired(false);

        // Chave estrangeira com Campaign
        builder.HasOne<Campaign>()
            .WithMany()
            .HasForeignKey(m => m.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
