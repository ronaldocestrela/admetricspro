using Master.Domain.Tenants;
using Microsoft.EntityFrameworkCore;

namespace Master.Infrastructure.Persistence;

/// <summary>
/// Central catalog context containing tenant metadata and connection information.
/// </summary>
public sealed class MasterDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MasterDbContext"/> class.
    /// </summary>
    /// <param name="options">Configured options for this context.</param>
    public MasterDbContext(DbContextOptions<MasterDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets the tenant catalog set.
    /// </summary>
    public DbSet<Tenant> Tenants => Set<Tenant>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MasterDbContext).Assembly);
    }
}