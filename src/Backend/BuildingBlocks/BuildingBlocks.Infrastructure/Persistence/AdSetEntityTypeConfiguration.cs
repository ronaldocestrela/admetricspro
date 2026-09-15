using BuildingBlocks.Domain.Campaigns;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Configuração de mapeamento EF Core fluente para a entidade <see cref="AdSet"/>.
/// </summary>
public sealed class AdSetEntityTypeConfiguration : IEntityTypeConfiguration<AdSet>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AdSet> builder)
    {
        builder.ToTable("AdSets");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.CampaignId)
            .IsRequired();

        builder.Property(s => s.ConnectedAdAccountId)
            .IsRequired();

        builder.Property(s => s.ExternalAdSetId)
            .HasMaxLength(150)
            .IsRequired();

        builder.HasIndex(s => new { s.CampaignId, s.ExternalAdSetId })
            .IsUnique();

        builder.Property(s => s.Name)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(s => s.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(s => s.BidStrategy)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(s => s.OptimizationGoal)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(s => s.DailyBudget)
            .HasPrecision(18, 2)
            .IsRequired(false);

        builder.Property(s => s.LifetimeBudget)
            .HasPrecision(18, 2)
            .IsRequired(false);

        builder.Property(s => s.TargetingSummary)
            .HasMaxLength(4000)
            .IsRequired(false);

        builder.Property(s => s.StartDateUtc)
            .IsRequired(false);

        builder.Property(s => s.EndDateUtc)
            .IsRequired(false);

        builder.Property(s => s.LastSyncedAtUtc)
            .IsRequired();

        builder.Property(s => s.CreatedAtUtc)
            .IsRequired();

        builder.Property(s => s.UpdatedAtUtc)
            .IsRequired(false);

        builder.HasMany(s => s.Ads)
            .WithOne()
            .HasForeignKey(ad => ad.AdSetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
