using BuildingBlocks.Domain.Tenants;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Base operational DbContext representing a dedicated tenant database instance in the database-per-tenant architecture.
/// </summary>
public class TenantDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantDbContext"/> class with the specified options.
    /// </summary>
    /// <param name="options">Configured options containing the tenant's connection string.</param>
    public TenantDbContext(DbContextOptions options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets the schema marker table used to validate schema provisioning and migrations.
    /// </summary>
    public DbSet<TenantSchemaMarker> TenantSchemaMarkers => Set<TenantSchemaMarker>();

    /// <summary>
    /// Gets the operational tenant users table.
    /// </summary>
    public DbSet<TenantUser> TenantUsers => Set<TenantUser>();

    /// <summary>
    /// Gets the operational tenant branding configuration table.
    /// </summary>
    public DbSet<TenantBranding> TenantBranding => Set<TenantBranding>();

    /// <summary>
    /// Gets the operational workspaces table representing agency clients.
    /// </summary>
    public DbSet<Workspace> Workspaces => Set<Workspace>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TenantSchemaMarker>(builder =>
        {
            builder.ToTable("TenantSchemaMarkers");
            builder.HasKey(marker => marker.Id);
            builder.Property(marker => marker.Name).HasMaxLength(200).IsRequired();
        });

        modelBuilder.ApplyConfiguration(new TenantUserEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new TenantBrandingEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new WorkspaceEntityTypeConfiguration());
    }
}
