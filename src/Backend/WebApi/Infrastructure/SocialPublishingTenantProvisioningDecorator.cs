using Analytics.Application.Abstractions;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Services;
using Master.Domain.Tenants;
using Master.Infrastructure.Services;
using SocialPublishing.Application.Abstractions;

namespace WebApi.Infrastructure;

/// <summary>
/// Decorator de provisionamento que aplica migrações do schema social após o pipeline operacional do Master.
/// </summary>
public sealed class SocialPublishingTenantProvisioningDecorator : ITenantProvisioningService
{
    private readonly TenantProvisioningService _inner;
    private readonly ISocialPublishingSchemaMigrator _schemaMigrator;
    private readonly IAnalyticsSchemaMigrator _analyticsSchemaMigrator;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="SocialPublishingTenantProvisioningDecorator"/>.
    /// </summary>
    /// <param name="inner">Serviço de provisionamento operacional do módulo Master.</param>
    /// <param name="schemaMigrator">Migrador do schema social.</param>
    /// <param name="analyticsSchemaMigrator">Migrador do schema analytics.</param>
    public SocialPublishingTenantProvisioningDecorator(
        TenantProvisioningService inner,
        ISocialPublishingSchemaMigrator schemaMigrator,
        IAnalyticsSchemaMigrator analyticsSchemaMigrator)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _schemaMigrator = schemaMigrator ?? throw new ArgumentNullException(nameof(schemaMigrator));
        _analyticsSchemaMigrator = analyticsSchemaMigrator ?? throw new ArgumentNullException(nameof(analyticsSchemaMigrator));
    }

    /// <inheritdoc />
    public async Task<Result<TenantId>> ProvisionTenantDatabaseAsync(
        ProvisionTenantCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _inner.ProvisionTenantDatabaseAsync(command, cancellationToken);
        if (result.IsFailure)
        {
            return result;
        }

        var migrationResult = await _schemaMigrator.ApplyForTenantAsync(result.Value.Value, cancellationToken);
        if (migrationResult.IsFailure)
        {
            return Result<TenantId>.Failure(migrationResult.Error);
        }

        var analyticsMigrationResult = await _analyticsSchemaMigrator.ApplyForTenantAsync(result.Value.Value, cancellationToken);
        if (analyticsMigrationResult.IsFailure)
        {
            return Result<TenantId>.Failure(analyticsMigrationResult.Error);
        }

        return result;
    }

    /// <inheritdoc />
    public Task<Result<TenantId>> ProvisionTenantDatabaseAsync(
        string companyName,
        string cnpj,
        string subdomain,
        CancellationToken cancellationToken)
    {
        return ProvisionTenantDatabaseAsync(
            new ProvisionTenantCommand(companyName, cnpj, subdomain),
            cancellationToken);
    }
}
