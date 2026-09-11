using BuildingBlocks.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Configuração de mapeamento do EF Core para a entidade imutável de auditoria <see cref="TenantAuditLog"/>
/// no banco de dados operacional dedicado do inquilino.
/// </summary>
public sealed class TenantAuditLogEntityTypeConfiguration : IEntityTypeConfiguration<TenantAuditLog>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TenantAuditLog> builder)
    {
        builder.ToTable("TenantAuditLogs");

        builder.HasKey(entry => entry.Id);

        builder
            .Property(entry => entry.UserId)
            .IsRequired();

        builder
            .Property(entry => entry.UserEmail)
            .HasMaxLength(256)
            .IsRequired();

        builder
            .Property(entry => entry.Action)
            .HasMaxLength(150)
            .IsRequired();

        builder
            .Property(entry => entry.Resource)
            .HasMaxLength(100)
            .IsRequired();

        builder
            .Property(entry => entry.ResourceId)
            .HasMaxLength(200);

        builder
            .Property(entry => entry.Details)
            .HasMaxLength(4000);

        builder
            .Property(entry => entry.IpAddress)
            .HasMaxLength(45);

        builder
            .Property(entry => entry.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(entry => entry.UserId);
        builder.HasIndex(entry => entry.Action);
        builder.HasIndex(entry => entry.CreatedAtUtc);
        builder.HasIndex(entry => new { entry.Resource, entry.ResourceId });
    }
}
