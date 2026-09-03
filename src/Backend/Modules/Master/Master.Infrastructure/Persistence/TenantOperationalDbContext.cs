using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Master.Infrastructure.Persistence;

/// <summary>
/// Minimal operational tenant context used during initial tenant provisioning.
/// </summary>
public sealed class TenantOperationalDbContext : TenantDbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantOperationalDbContext"/> class.
    /// </summary>
    /// <param name="options">Configured options.</param>
    public TenantOperationalDbContext(DbContextOptions<TenantOperationalDbContext> options)
        : base(options)
    {
    }
}