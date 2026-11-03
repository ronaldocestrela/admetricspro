using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SocialPublishing.Domain.Brand;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Posts;

namespace SocialPublishing.Infrastructure.Persistence;

/// <summary>
/// Contexto EF Core exclusivo do bounded context SocialPublishing no banco dedicado do tenant.
/// </summary>
public sealed class SocialPublishingDbContext : DbContext
{
    /// <summary>
    /// Inicializa uma nova instância de <see cref="SocialPublishingDbContext"/>.
    /// </summary>
    /// <param name="options">Opções configuradas com connection string e histórico de migrações do schema social.</param>
    public SocialPublishingDbContext(DbContextOptions<SocialPublishingDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Obtém o conjunto de conexões orgânicas.
    /// </summary>
    public DbSet<OrganicConnection> OrganicConnections => Set<OrganicConnection>();

    /// <summary>
    /// Obtém o conjunto de posts orgânicos.
    /// </summary>
    public DbSet<OrganicPost> OrganicPosts => Set<OrganicPost>();

    /// <summary>
    /// Obtém o conjunto de ativos de mídia orgânica (standalone ou filhos de post).
    /// </summary>
    public DbSet<OrganicMediaAsset> OrganicMediaAssets => Set<OrganicMediaAsset>();

    /// <summary>
    /// Obtém o conjunto de perfis de voz de marca.
    /// </summary>
    public DbSet<BrandVoiceProfile> BrandVoiceProfiles => Set<BrandVoiceProfile>();

    /// <summary>
    /// Cria opções SQL Server com schema padrão <c>social</c> e histórico de migrações isolado.
    /// </summary>
    /// <param name="connectionString">Connection string do banco do tenant.</param>
    /// <returns>Opções prontas para instanciar o contexto.</returns>
    public static DbContextOptions<SocialPublishingDbContext> CreateOptions(string connectionString)
    {
        return new DbContextOptionsBuilder<SocialPublishingDbContext>()
            .UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "social");
                sql.MigrationsAssembly(typeof(SocialPublishingDbContext).Assembly.FullName);
            })
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("social");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SocialPublishingDbContext).Assembly);
    }
}
