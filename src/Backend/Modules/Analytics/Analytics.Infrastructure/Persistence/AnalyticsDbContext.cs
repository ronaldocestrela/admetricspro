using Analytics.Domain.Creatives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Analytics.Infrastructure.Persistence;

/// <summary>
/// Contexto EF Core do bounded context Analytics no banco dedicado do tenant.
/// </summary>
public sealed class AnalyticsDbContext : DbContext
{
    /// <summary>
    /// Inicializa uma nova instância de <see cref="AnalyticsDbContext"/>.
    /// </summary>
    /// <param name="options">Opções com connection string e histórico de migrações do schema analytics.</param>
    public AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Obtém o conjunto de ativos persistidos do Creative Hub.
    /// </summary>
    public DbSet<CreativeHubAsset> CreativeHubAssets => Set<CreativeHubAsset>();

    /// <summary>
    /// Cria opções SQL Server com schema padrão <c>analytics</c>.
    /// </summary>
    /// <param name="connectionString">Connection string do tenant.</param>
    /// <returns>Opções prontas para instanciar o contexto.</returns>
    public static DbContextOptions<AnalyticsDbContext> CreateOptions(string connectionString)
    {
        return new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "analytics");
                sql.MigrationsAssembly(typeof(AnalyticsDbContext).Assembly.FullName);
            })
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("analytics");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AnalyticsDbContext).Assembly);
    }
}
