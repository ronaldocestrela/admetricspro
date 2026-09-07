using BuildingBlocks.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// EF Core entity type configuration for operational <see cref="TenantUser"/>.
/// </summary>
public sealed class TenantUserEntityTypeConfiguration : IEntityTypeConfiguration<TenantUser>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TenantUser> builder)
    {
        builder.ToTable("TenantUsers");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(user => user.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(user => user.Email)
            .IsUnique();

        builder.Property(user => user.PhoneNumber)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(user => user.PasswordHash)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(user => user.Role)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(user => user.IsActive)
            .IsRequired();

        builder.Property(user => user.CreatedAtUtc)
            .IsRequired();

        builder.Property(user => user.UpdatedAtUtc)
            .IsRequired(false);
    }
}
