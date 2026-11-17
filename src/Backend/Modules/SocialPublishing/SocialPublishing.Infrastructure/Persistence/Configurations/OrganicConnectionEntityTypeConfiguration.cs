using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialPublishing.Domain.Connections;

namespace SocialPublishing.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento EF Core de <see cref="OrganicConnection"/> no schema social.
/// </summary>
public sealed class OrganicConnectionEntityTypeConfiguration : IEntityTypeConfiguration<OrganicConnection>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<OrganicConnection> builder)
    {
        builder.ToTable("OrganicConnections");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.WorkspaceId).IsRequired();
        builder.Property(c => c.Platform)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(c => c.ExternalAccountId).HasMaxLength(200).IsRequired();
        builder.Property(c => c.DisplayName).HasMaxLength(300).IsRequired();
        builder.Property(c => c.EncryptedAccessToken).HasMaxLength(4000).IsRequired();
        builder.Property(c => c.EncryptedRefreshToken).HasMaxLength(4000);
        builder.Property(c => c.AccessTokenExpiresAtUtc);
        builder.Property(c => c.Scopes).HasMaxLength(2000).IsRequired();
        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(c => c.CreatedAtUtc).IsRequired();

        builder.HasIndex(c => new { c.WorkspaceId, c.Platform, c.ExternalAccountId }).IsUnique();
        builder.HasIndex(c => c.WorkspaceId);
    }
}
