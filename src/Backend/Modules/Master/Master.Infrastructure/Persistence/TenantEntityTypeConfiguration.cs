using Master.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Master.Infrastructure.Persistence;

/// <summary>
/// EF Core mapping for tenant catalog aggregate.
/// </summary>
public sealed class TenantEntityTypeConfiguration : IEntityTypeConfiguration<Tenant>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");

        builder.HasKey(tenant => tenant.Id);
        builder
            .Property(tenant => tenant.Id)
            .HasConversion(id => id.Value, value => new TenantId(value));

        builder
            .Property(tenant => tenant.CompanyName)
            .HasMaxLength(200)
            .IsRequired();

        builder
            .Property(tenant => tenant.Cnpj)
            .HasMaxLength(14)
            .IsRequired();

        builder
            .HasIndex(tenant => tenant.Cnpj)
            .IsUnique();

        builder
            .Property(tenant => tenant.Subdomain)
            .HasMaxLength(80)
            .IsRequired();

        builder
            .HasIndex(tenant => tenant.Subdomain)
            .IsUnique();

        builder
            .Property(tenant => tenant.EncryptedConnectionString)
            .HasMaxLength(2000)
            .IsRequired();

        builder
            .Property(tenant => tenant.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder
            .Property(tenant => tenant.CreatedAtUtc)
            .IsRequired();
    }
}