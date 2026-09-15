using BuildingBlocks.Domain.Campaigns;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Configuração de mapeamento EF Core fluente para a entidade <see cref="Campaign"/>.
/// </summary>
public sealed class CampaignEntityTypeConfiguration : IEntityTypeConfiguration<Campaign>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Campaign> builder)
    {
        builder.ToTable("Campaigns");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.WorkspaceId)
            .IsRequired();

        builder.HasIndex(c => c.WorkspaceId);

        builder.Property(c => c.ConnectedAdAccountId)
            .IsRequired();

        builder.Property(c => c.Platform)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.ExternalCampaignId)
            .HasMaxLength(150)
            .IsRequired();

        builder.HasIndex(c => new { c.ConnectedAdAccountId, c.ExternalCampaignId })
            .IsUnique();

        builder.Property(c => c.Name)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(c => c.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(c => c.Objective)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.DailyBudget)
            .HasPrecision(18, 2)
            .IsRequired(false);

        builder.Property(c => c.LifetimeBudget)
            .HasPrecision(18, 2)
            .IsRequired(false);

        builder.Property(c => c.Currency)
            .HasMaxLength(10)
            .HasDefaultValue("BRL")
            .IsRequired();

        builder.Property(c => c.StartDateUtc)
            .IsRequired(false);

        builder.Property(c => c.EndDateUtc)
            .IsRequired(false);

        builder.Property(c => c.LastSyncedAtUtc)
            .IsRequired();

        builder.Property(c => c.CreatedAtUtc)
            .IsRequired();

        builder.Property(c => c.UpdatedAtUtc)
            .IsRequired(false);

        builder.HasMany(c => c.AdSets)
            .WithOne()
            .HasForeignKey(adSet => adSet.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
