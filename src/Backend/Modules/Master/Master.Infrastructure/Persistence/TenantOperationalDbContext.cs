using Microsoft.EntityFrameworkCore;

namespace Master.Infrastructure.Persistence;

/// <summary>
/// Minimal operational tenant context used during initial tenant provisioning.
/// </summary>
public sealed class TenantOperationalDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantOperationalDbContext"/> class.
    /// </summary>
    /// <param name="options">Configured options.</param>
    public TenantOperationalDbContext(DbContextOptions<TenantOperationalDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets a marker table to ensure schema creation can be validated in tests.
    /// </summary>
    public DbSet<TenantSchemaMarker> TenantSchemaMarkers => Set<TenantSchemaMarker>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenantSchemaMarker>(builder =>
        {
            builder.ToTable("TenantSchemaMarkers");
            builder.HasKey(marker => marker.Id);
            builder.Property(marker => marker.Name).HasMaxLength(200).IsRequired();
        });
    }
}

/// <summary>
/// Marker entity used to verify tenant schema provisioning in integration tests.
/// </summary>
public sealed class TenantSchemaMarker
{
    /// <summary>
    /// Gets or sets marker identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets marker name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}