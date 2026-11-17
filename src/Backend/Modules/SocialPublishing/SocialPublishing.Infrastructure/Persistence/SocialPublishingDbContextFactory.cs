using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace SocialPublishing.Infrastructure.Persistence;

/// <summary>
/// Fábrica que instancia <see cref="SocialPublishingDbContext"/> com connection string resolvida por tenant.
/// </summary>
public sealed class SocialPublishingDbContextFactory : ITenantDbContextFactory<SocialPublishingDbContext>
{
    private readonly ITenantConnectionResolver _connectionResolver;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="SocialPublishingDbContextFactory"/>.
    /// </summary>
    /// <param name="connectionResolver">Resolvedor de connection string do tenant.</param>
    public SocialPublishingDbContextFactory(ITenantConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver ?? throw new ArgumentNullException(nameof(connectionResolver));
    }

    /// <inheritdoc />
    public async Task<Result<SocialPublishingDbContext>> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        var connectionStringResult = await _connectionResolver.ResolveCurrentTenantConnectionStringAsync(cancellationToken);
        if (connectionStringResult.IsFailure)
        {
            return Result<SocialPublishingDbContext>.Failure(connectionStringResult.Error);
        }

        return CreateContext(connectionStringResult.Value);
    }

    /// <inheritdoc />
    public async Task<Result<SocialPublishingDbContext>> CreateDbContextAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var connectionStringResult = await _connectionResolver.ResolveConnectionStringAsync(tenantId, cancellationToken);
        if (connectionStringResult.IsFailure)
        {
            return Result<SocialPublishingDbContext>.Failure(connectionStringResult.Error);
        }

        return CreateContext(connectionStringResult.Value);
    }

    private static Result<SocialPublishingDbContext> CreateContext(string connectionString)
    {
        try
        {
            var options = SocialPublishingDbContext.CreateOptions(connectionString);
            return Result<SocialPublishingDbContext>.Success(new SocialPublishingDbContext(options));
        }
        catch (Exception ex)
        {
            return Result<SocialPublishingDbContext>.Failure(
                Error.Failure("Social.DbContextCreationFailed", $"Falha ao criar SocialPublishingDbContext: {ex.Message}"));
        }
    }
}
