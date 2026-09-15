using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using Integrations.Domain.Campaigns.Models;

namespace Integrations.Domain.Campaigns.Sync;

/// <summary>
/// Despachante central para orquestração da sincronização estrutural de campanhas,
/// selecionando dinamicamente o adaptador adequado conforme a plataforma e o status (demo vs real).
/// </summary>
public interface ICampaignHierarchySyncDispatcher
{
    /// <summary>
    /// Despacha a sincronização estrutural de uma conta de anúncios para o adaptador correspondente.
    /// </summary>
    /// <param name="account">Conta de anúncios conectada.</param>
    /// <param name="decryptedAccessToken">Token descriptografado, caso seja conta real.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Estrutura unificada de campanhas, conjuntos e anúncios.</returns>
    Task<Result<UnifiedCampaignHierarchy>> DispatchAsync(
        ConnectedAdAccount account,
        string? decryptedAccessToken,
        CancellationToken cancellationToken = default);
}
