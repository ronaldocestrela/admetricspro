using Master.Domain.Auditing;
using Master.Domain.Plans;
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

    /// <summary>
    /// Gets the subscription plans catalog set.
    /// </summary>
    public DbSet<SubscriptionPlan> Plans => Set<SubscriptionPlan>();

    /// <summary>
    /// Gets the impersonation sessions catalog set.
    /// </summary>
    public DbSet<ImpersonationSession> ImpersonationSessions => Set<ImpersonationSession>();

    /// <summary>
    /// Gets the master audit logs catalog set.
    /// </summary>
    public DbSet<MasterAuditEntry> AuditLogs => Set<MasterAuditEntry>();

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MasterDbContext).Assembly);
    }
}