using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialPublishing.Domain.Approvals;

namespace SocialPublishing.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento EF Core de <see cref="ApprovalLink"/> no schema social.
/// </summary>
public sealed class ApprovalLinkEntityTypeConfiguration : IEntityTypeConfiguration<ApprovalLink>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ApprovalLink> builder)
    {
        builder.ToTable("ApprovalLinks");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.WorkspaceId).IsRequired();
        builder.Property(l => l.TokenHash).HasMaxLength(64).IsRequired();
        builder.Property(l => l.ExpiresAtUtc).IsRequired();
        builder.Property(l => l.RevokedAtUtc);
        builder.Property(l => l.CreatedAtUtc).IsRequired();

        builder.HasIndex(l => l.TokenHash).IsUnique();
        builder.HasIndex(l => new { l.WorkspaceId, l.RevokedAtUtc });
    }
}
