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

    /// <summary>
    /// Gets the operational squads table representing agency internal teams.
    /// </summary>
    public DbSet<Squad> Squads => Set<Squad>();

    /// <summary>
    /// Gets the operational squad members associative table.
    /// </summary>
    public DbSet<SquadMember> SquadMembers => Set<SquadMember>();

    /// <summary>
    /// Gets the operational squad workspaces associative table.
    /// </summary>
    public DbSet<SquadWorkspace> SquadWorkspaces => Set<SquadWorkspace>();

    /// <summary>
    /// Gets the operational connected ad accounts table representing marketing channels (Meta, Google, TikTok, Bing).
    /// </summary>
    public DbSet<ConnectedAdAccount> ConnectedAdAccounts => Set<ConnectedAdAccount>();

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
        modelBuilder.ApplyConfiguration(new SquadEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new SquadMemberEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new SquadWorkspaceEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new ConnectedAdAccountEntityTypeConfiguration());
    }
}
