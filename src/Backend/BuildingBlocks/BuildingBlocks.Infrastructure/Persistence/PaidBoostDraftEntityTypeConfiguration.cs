using BuildingBlocks.Domain.Integrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Mapeamento EF Core de <see cref="PaidBoostDraft"/>.
/// </summary>
public sealed class PaidBoostDraftEntityTypeConfiguration : IEntityTypeConfiguration<PaidBoostDraft>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PaidBoostDraft> builder)
    {
        builder.ToTable("PaidBoostDrafts");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Platform).HasMaxLength(32).IsRequired();
        builder.Property(d => d.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(d => d.Body).HasMaxLength(4000).IsRequired();
        builder.Property(d => d.MediaObjectKey).HasMaxLength(500);
        builder.Property(d => d.ExternalAdId).HasMaxLength(150).IsRequired();
        builder.Property(d => d.CreatedAtUtc).IsRequired();
        builder.HasIndex(d => new { d.WorkspaceId, d.SuggestionId }).IsUnique();
    }
}
