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
    }
}
