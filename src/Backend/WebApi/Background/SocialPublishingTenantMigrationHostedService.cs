using Master.Application.Tenants.Queries.GetTenants;
using MediatR;
using SocialPublishing.Application.Abstractions;

namespace WebApi.Background;

/// <summary>
/// Aplica migrações pendentes do schema social em todos os tenants ativos no startup do host.
/// </summary>
public sealed class SocialPublishingTenantMigrationHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SocialPublishingTenantMigrationHostedService> _logger;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="SocialPublishingTenantMigrationHostedService"/>.
    /// </summary>
    /// <param name="serviceProvider">Provedor de serviços raiz.</param>
    /// <param name="environment">Ambiente de hospedagem.</param>
    /// <param name="configuration">Configuração da aplicação.</param>
    /// <param name="logger">Logger estruturado.</param>
    public SocialPublishingTenantMigrationHostedService(
        IServiceProvider serviceProvider,
        IHostEnvironment environment,
        IConfiguration configuration,
        ILogger<SocialPublishingTenantMigrationHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _environment = environment;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_environment.IsEnvironment("Testing"))
        {
            return;
        }

        if (!_configuration.GetValue<bool>("DatabaseMigrations:ApplyMasterMigrationsOnStartup", false))
        {
            return;
        }

        _logger.LogInformation("Iniciando migrações do schema social para tenants ativos...");

        using var scope = _serviceProvider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var migrator = scope.ServiceProvider.GetRequiredService<ISocialPublishingSchemaMigrator>();

        var tenantsResult = await sender.Send(new GetTenantsQuery(), cancellationToken);
        if (tenantsResult.IsFailure)
        {
            _logger.LogError(
                "Falha ao listar tenants para migração social: {Code} - {Message}",
                tenantsResult.Error.Code,
                tenantsResult.Error.Description);
            throw new InvalidOperationException($"Falha ao listar tenants: {tenantsResult.Error.Description}");
        }

        foreach (var tenant in tenantsResult.Value.Where(t => t.Status is "Active" or "Trial"))
        {
            var migrationResult = await migrator.ApplyForTenantAsync(tenant.Id, cancellationToken);
            if (migrationResult.IsFailure)
            {
                _logger.LogError(
                    "Migração social falhou para tenant {TenantId}: {Code} - {Message}",
                    tenant.Id,
                    migrationResult.Error.Code,
                    migrationResult.Error.Description);
                throw new InvalidOperationException(
                    $"Migração social falhou para tenant {tenant.Id}: {migrationResult.Error.Description}");
            }
        }

        _logger.LogInformation("Migrações do schema social concluídas para tenants elegíveis.");
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
