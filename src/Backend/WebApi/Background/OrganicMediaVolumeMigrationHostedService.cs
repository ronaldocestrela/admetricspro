using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Infrastructure.MultiTenancy;
using BuildingBlocks.Infrastructure.Storage;
using Master.Application.Tenants.Queries.GetTenants;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Media;
using SocialPublishing.Application.Options;
using SocialPublishing.Infrastructure.Media;

namespace WebApi.Background;

/// <summary>
/// Processo one-shot que copia o volume legado de mídia orgânica para o MinIO e encerra o host (Fase 5).
/// </summary>
public sealed class OrganicMediaVolumeMigrationHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHostEnvironment _environment;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ObjectStorageOptions _objectStorageOptions;
    private readonly OrganicMediaOptions _mediaOptions;
    private readonly ILogger<OrganicMediaVolumeMigrationHostedService> _logger;

    /// <summary>
    /// Inicializa o serviço de migração de volume orgânico.
    /// </summary>
    /// <param name="scopeFactory">Fábrica de escopos por tenant.</param>
    /// <param name="environment">Ambiente de hospedagem.</param>
    /// <param name="lifetime">Controle de parada do host.</param>
    /// <param name="objectStorageOptions">Opções de object storage.</param>
    /// <param name="mediaOptions">Raiz local legada.</param>
    /// <param name="logger">Logger estruturado.</param>
    public OrganicMediaVolumeMigrationHostedService(
        IServiceScopeFactory scopeFactory,
        IHostEnvironment environment,
        IHostApplicationLifetime lifetime,
        IOptions<ObjectStorageOptions> objectStorageOptions,
        IOptions<OrganicMediaOptions> mediaOptions,
        ILogger<OrganicMediaVolumeMigrationHostedService> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
        _objectStorageOptions = objectStorageOptions?.Value ?? throw new ArgumentNullException(nameof(objectStorageOptions));
        _mediaOptions = mediaOptions?.Value ?? throw new ArgumentNullException(nameof(mediaOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken) =>
        _environment.IsEnvironment("Testing")
            ? Task.CompletedTask
            : RunMigrationAsync(cancellationToken);

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task RunMigrationAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var catalogScope = _scopeFactory.CreateScope();
            var sender = catalogScope.ServiceProvider.GetRequiredService<ISender>();
            var tenants = await sender.Send(new GetTenantsQuery(), cancellationToken);
            if (tenants.IsFailure)
            {
                _logger.LogError(
                    "Migração de volume orgânico abortada: catálogo indisponível ({Code}).",
                    tenants.Error.Code);
                Environment.ExitCode = 1;
                _lifetime.StopApplication();
                return;
            }

            var mimeByKey = new Dictionary<string, string>(StringComparer.Ordinal);
            var catalogEntries = new List<OrganicMediaCatalogEntry>();

            foreach (var tenant in tenants.Value.Where(t => t.Status is "Active" or "Trial"))
            {
                await using var tenantScope = _scopeFactory.CreateAsyncScope();
                var accessor = tenantScope.ServiceProvider.GetRequiredService<ITenantContextAccessor>();
                accessor.TenantContext = TenantContext.Create(
                    tenant.Id,
                    tenant.Subdomain,
                    TenantResolutionSource.None);

                var repository = tenantScope.ServiceProvider.GetRequiredService<IOrganicMediaAssetRepository>();
                var entries = await repository.ListObjectKeysForMigrationAuditAsync(cancellationToken);
                catalogEntries.AddRange(entries);

                foreach (var entry in entries)
                {
                    if (!string.IsNullOrWhiteSpace(entry.ObjectKey))
                    {
                        mimeByKey[entry.ObjectKey] = entry.MimeType ?? "application/octet-stream";
                    }
                }
            }

            using var workScope = _scopeFactory.CreateScope();
            var copier = workScope.ServiceProvider.GetRequiredService<OrganicMediaVolumeCopier>();
            string ResolveMime(string key) =>
                mimeByKey.TryGetValue(key, out var mime) ? mime : "application/octet-stream";

            var copy = await copier.CopyFromLocalVolumeAsync(
                _mediaOptions.LocalRoot,
                ResolveMime,
                cancellationToken);

            if (copy.IsFailure)
            {
                _logger.LogError(
                    "Migração de volume orgânico falhou: {Code} - {Description}",
                    copy.Error.Code,
                    copy.Error.Description);
                Environment.ExitCode = 1;
                _lifetime.StopApplication();
                return;
            }

            var missingOnDisk = copier.CountMissingOnDisk(catalogEntries, _mediaOptions.LocalRoot);
            var report = copy.Value with { MissingOnDisk = missingOnDisk };

            _logger.LogInformation(
                "Migração de volume orgânico concluída. Copiados={Copied}, Ignorados={Skipped}, Falhos={Failed}, AusentesNoDisco={MissingOnDisk}",
                report.Copied,
                report.Skipped,
                report.Failed,
                report.MissingOnDisk);

            Environment.ExitCode = report.Failed > 0 ? 1 : 0;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Migração de volume orgânico encerrada com exceção inesperada.");
            Environment.ExitCode = 1;
        }
        finally
        {
            _lifetime.StopApplication();
        }
    }
}