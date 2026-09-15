using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using Integrations.Domain.Campaigns.Models;
using Integrations.Domain.Campaigns.Sync;

namespace Integrations.Infrastructure.Campaigns.Sync;

/// <summary>
/// Implementação concreta do despachante central de sincronização hierárquica.
/// Seleciona o adaptador correspondente à plataforma ou o adaptador de simulação demo.
/// </summary>
public sealed class CampaignHierarchySyncDispatcher : ICampaignHierarchySyncDispatcher
{
    private readonly IEnumerable<ICampaignHierarchySyncAdapter> _adapters;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="CampaignHierarchySyncDispatcher"/>.
    /// </summary>
    /// <param name="adapters">Coleção de adaptadores de plataforma registrados.</param>
    public CampaignHierarchySyncDispatcher(IEnumerable<ICampaignHierarchySyncAdapter> adapters)
    {
        _adapters = adapters ?? throw new ArgumentNullException(nameof(adapters));
    }

    /// <inheritdoc />
    public async Task<Result<UnifiedCampaignHierarchy>> DispatchAsync(
        ConnectedAdAccount account,
        string? decryptedAccessToken,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(account);

        // Se a conta for demonstrativa, roteia diretamente para o adaptador Demo
        if (account.IsDemo)
        {
            var demoAdapter = _adapters.FirstOrDefault(a => a.Platform.Equals("Demo", StringComparison.OrdinalIgnoreCase));
            if (demoAdapter is null)
            {
                return Result<UnifiedCampaignHierarchy>.Failure(
                    Error.NotFound("Sync.DemoAdapterNotFound", "Adaptador para contas de demonstração não registrado."));
            }

            return await demoAdapter.FetchHierarchyAsync(account, decryptedAccessToken, cancellationToken);
        }

        // Roteia para o adaptador da plataforma correspondente
        var adapter = _adapters.FirstOrDefault(a => a.Platform.Equals(account.Platform, StringComparison.OrdinalIgnoreCase));
        if (adapter is null)
        {
            return Result<UnifiedCampaignHierarchy>.Failure(
                Error.NotFound("Sync.AdapterNotFound", $"Nenhum adaptador registrado para a plataforma '{account.Platform}'."));
        }

        return await adapter.FetchHierarchyAsync(account, decryptedAccessToken, cancellationToken);
    }
}
