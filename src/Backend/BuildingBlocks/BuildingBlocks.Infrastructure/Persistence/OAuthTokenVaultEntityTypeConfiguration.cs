using BuildingBlocks.Domain.Integrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Configuração EF Core fluente para a entidade <see cref="OAuthTokenVault"/> no banco dedicado do inquilino.
/// </summary>
public sealed class OAuthTokenVaultEntityTypeConfiguration : IEntityTypeConfiguration<OAuthTokenVault>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<OAuthTokenVault> builder)
    {
        builder.ToTable("OAuthTokenVaults");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.WorkspaceId)
            .IsRequired();

        builder.Property(v => v.Platform)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(v => new { v.WorkspaceId, v.Platform })
            .IsUnique();

        builder.Property(v => v.ExternalAccountId)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(v => v.ExternalAccountName)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(v => v.EncryptedAccessToken)
            .IsRequired();

        builder.Property(v => v.EncryptedRefreshToken);

        builder.Property(v => v.AccessTokenExpiresAtUtc);

        builder.Property(v => v.RefreshTokenExpiresAtUtc);

        builder.Property(v => v.Status)
            .HasMaxLength(50)
            .HasDefaultValue(OAuthConnectionStatus.Active)
            .IsRequired();

        builder.Property(v => v.Scopes)
            .HasMaxLength(1000)
            .HasDefaultValue(string.Empty)
            .IsRequired();

        builder.Property(v => v.CreatedAtUtc)
            .IsRequired();

        builder.Property(v => v.UpdatedAtUtc);
    }
}
