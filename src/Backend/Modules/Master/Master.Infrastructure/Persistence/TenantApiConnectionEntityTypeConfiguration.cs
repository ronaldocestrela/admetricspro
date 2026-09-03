using Master.Domain.Integrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Master.Infrastructure.Persistence;

/// <summary>
/// EF Core entity configuration for <see cref="TenantApiConnection"/> mapping to table 'TenantApiConnections'.
/// </summary>
public sealed class TenantApiConnectionEntityTypeConfiguration : IEntityTypeConfiguration<TenantApiConnection>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TenantApiConnection> builder)
    {
        builder.ToTable("TenantApiConnections");

        builder.HasKey(c => c.Id);

        builder
            .Property(c => c.TenantId)
            .IsRequired();

        builder
            .Property(c => c.TenantName)
            .HasMaxLength(200)
            .IsRequired();

        builder
            .Property(c => c.Platform)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder
            .Property(c => c.AccountIdentifier)
            .HasMaxLength(100)
            .IsRequired();

        builder
            .Property(c => c.AccountName)
            .HasMaxLength(200)
            .IsRequired();

        builder
            .Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder
            .Property(c => c.TokenExpiresAtUtc);

        builder
            .Property(c => c.LastSyncAtUtc);

        builder
            .Property(c => c.ErrorMessage)
            .HasMaxLength(1000);

        builder
            .Property(c => c.CreatedAtUtc)
            .IsRequired();

        builder
            .Property(c => c.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(c => new { c.TenantId, c.Platform });
        builder.HasIndex(c => new { c.Status, c.Platform });
    }
}
