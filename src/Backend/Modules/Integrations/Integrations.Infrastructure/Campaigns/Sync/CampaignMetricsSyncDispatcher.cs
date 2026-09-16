using BuildingBlocks.Domain.Campaigns;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using Integrations.Domain.Campaigns.Models;
using Integrations.Domain.Campaigns.Sync;

namespace Integrations.Infrastructure.Campaigns.Sync;

/// <summary>
/// Despachante central de sincronização de métricas analíticas.
/// Seleciona dinamicamente o adaptador adequado conforme a plataforma ou modo demonstrativo da conta.
/// </summary>
public sealed class CampaignMetricsSyncDispatcher : ICampaignMetricsSyncDispatcher
{
    private readonly IEnumerable<ICampaignMetricsSyncAdapter> _adapters;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="CampaignMetricsSyncDispatcher"/>.
    /// </summary>
    /// <param name="adapters">Coleção de adaptadores de métricas registrados no contêiner.</param>
    public CampaignMetricsSyncDispatcher(IEnumerable<ICampaignMetricsSyncAdapter> adapters)
    {
        _adapters = adapters ?? throw new ArgumentNullException(nameof(adapters));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<UnifiedMetricItem>>> DispatchAsync(
        ConnectedAdAccount account,
        string? decryptedAccessToken,
        DateTime startDateUtc,
        DateTime endDateUtc,
        MetricGranularity granularity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(account);

        // Se a conta for demonstrativa, roteia diretamente para o adaptador Demo
        if (account.IsDemo)
        {
            var demoAdapter = _adapters.FirstOrDefault(a => a.Platform.Equals("Demo", StringComparison.OrdinalIgnoreCase));
            if (demoAdapter is null)
            {
                return Result<IReadOnlyList<UnifiedMetricItem>>.Failure(
                    Error.NotFound("Sync.DemoMetricsAdapterNotFound", "Adaptador de métricas para contas demo não registrado."));
            }

            return await demoAdapter.FetchMetricsAsync(account, decryptedAccessToken, startDateUtc, endDateUtc, granularity, cancellationToken);
        }

        // Roteia para o adaptador especializado da plataforma
        var adapter = _adapters.FirstOrDefault(a => a.Platform.Equals(account.Platform, StringComparison.OrdinalIgnoreCase));
        if (adapter is null)
        {
            return Result<IReadOnlyList<UnifiedMetricItem>>.Failure(
                Error.NotFound("Sync.MetricsAdapterNotFound", $"Nenhum adaptador de métricas registrado para a plataforma '{account.Platform}'."));
        }

        return await adapter.FetchMetricsAsync(account, decryptedAccessToken, startDateUtc, endDateUtc, granularity, cancellationToken);
    }
}
