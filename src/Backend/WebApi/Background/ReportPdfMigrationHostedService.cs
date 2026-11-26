using Analytics.Infrastructure.Reports;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Infrastructure.MultiTenancy;
using BuildingBlocks.Infrastructure.Storage;
using Master.Application.Tenants.Queries.GetTenants;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WebApi.Background;

/// <summary>
/// Processo one-shot que copia PDFs legados do SQL para o MinIO e encerra o host (Fase 6).
/// </summary>
public sealed class ReportPdfMigrationHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHostEnvironment _environment;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ObjectStorageOptions _objectStorageOptions;
    private readonly ILogger<ReportPdfMigrationHostedService> _logger;

    /// <summary>
    /// Inicializa o serviço de migração de PDFs de relatório.
    /// </summary>
    public ReportPdfMigrationHostedService(
        IServiceScopeFactory scopeFactory,
        IHostEnvironment environment,
        IHostApplicationLifetime lifetime,
        IOptions<ObjectStorageOptions> objectStorageOptions,
        ILogger<ReportPdfMigrationHostedService> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
        _objectStorageOptions = objectStorageOptions?.Value ?? throw new ArgumentNullException(nameof(objectStorageOptions));
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
                    "Migração de PDFs de relatório abortada: catálogo indisponível ({Code}).",
                    tenants.Error.Code);
                Environment.ExitCode = 1;
                _lifetime.StopApplication();
                return;
            }

            var copied = 0;
            var skipped = 0;
            var failed = 0;

            foreach (var tenant in tenants.Value.Where(t => t.Status is "Active" or "Trial"))
            {
                await using var tenantScope = _scopeFactory.CreateAsyncScope();
                var accessor = tenantScope.ServiceProvider.GetRequiredService<BuildingBlocks.Application.MultiTenancy.ITenantContextAccessor>();
                accessor.TenantContext = TenantContext.Create(
                    tenant.Id,
                    tenant.Subdomain,
                    TenantResolutionSource.None);

                var copier = tenantScope.ServiceProvider.GetRequiredService<ReportPdfCopier>();
                var result = await copier.CopyTenantReportsAsync(cancellationToken);
                if (result.IsFailure)
                {
                    _logger.LogWarning(
                        "Migração de PDFs falhou no tenant {TenantId}: {Code}",
                        tenant.Id,
                        result.Error.Code);
                    failed++;
                    continue;
                }

                copied += result.Value.Copied;
                skipped += result.Value.Skipped;
                failed += result.Value.Failed;
            }

            _logger.LogInformation(
                "Migração de PDFs de relatório concluída. Copiados={Copied}, Ignorados={Skipped}, Falhos={Failed}",
                copied,
                skipped,
                failed);

            Environment.ExitCode = failed > 0 ? 1 : 0;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Migração de PDFs de relatório encerrada com exceção inesperada.");
            Environment.ExitCode = 1;
        }
        finally
        {
            _lifetime.StopApplication();
        }
    }
}