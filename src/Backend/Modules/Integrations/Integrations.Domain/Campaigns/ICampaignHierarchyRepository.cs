using BuildingBlocks.Domain.Campaigns;
using BuildingBlocks.Domain.Primitives;
using Integrations.Domain.Campaigns.Models;

namespace Integrations.Domain.Campaigns;

/// <summary>
/// Contrato do repositório de persistência da hierarquia universal de campanhas (Campaign -> AdSet -> Ad).
/// Opera no contexto do banco de dados dedicado do inquilino (TenantDbContext).
/// </summary>
public interface ICampaignHierarchyRepository
{
    /// <summary>
    /// Insere ou atualiza atomicamente em lote as campanhas, conjuntos e anúncios sincronizados de uma conta conectada.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace associado.</param>
    /// <param name="connectedAdAccountId">Identificador da conta conectada.</param>
    /// <param name="platform">Plataforma de anúncios (MetaAds, GoogleAds, TikTokAds, BingAds).</param>
    /// <param name="hierarchy">Estrutura unificada de campanhas, conjuntos e anúncios.</param>
    /// <param name="syncTimeUtc">Carimbo UTC da sincronização.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Quantidade total de entidades atualizadas ou inseridas.</returns>
    Task<Result<int>> UpsertHierarchyBatchAsync(
        Guid workspaceId,
        Guid connectedAdAccountId,
        string platform,
        UnifiedCampaignHierarchy hierarchy,
        DateTime syncTimeUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém a lista de campanhas do workspace com filtros opcionais.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="connectedAdAccountId">Filtro opcional por conta conectada.</param>
    /// <param name="platform">Filtro opcional por plataforma.</param>
    /// <param name="status">Filtro opcional por status.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Coleção de campanhas encontradas.</returns>
    Task<IReadOnlyList<Campaign>> GetCampaignsByWorkspaceAsync(
        Guid workspaceId,
        Guid? connectedAdAccountId = null,
        string? platform = null,
        string? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém uma campanha específica com seus conjuntos e anúncios carregados em memória.
    /// </summary>
    /// <param name="campaignId">Identificador da campanha.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Campanha com hierarquia completa ou nulo.</returns>
    Task<Campaign?> GetCampaignWithHierarchyByIdAsync(
        Guid campaignId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém as contas de anúncios conectadas do workspace aptas para sincronização estrutural.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="connectedAdAccountId">Filtro opcional por conta conectada específica.</param>
    /// <param name="platform">Filtro opcional por plataforma.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista de contas conectadas encontradas.</returns>
    Task<IReadOnlyList<BuildingBlocks.Domain.Tenants.ConnectedAdAccount>> GetAccountsForSyncAsync(
        Guid workspaceId,
        Guid? connectedAdAccountId = null,
        string? platform = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém uma campanha pelo seu identificador primário.
    /// </summary>
    /// <param name="campaignId">Identificador da campanha.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Campanha localizada ou nulo.</returns>
    Task<Campaign?> GetCampaignByIdAsync(
        Guid campaignId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém um anúncio pelo seu identificador primário.
    /// </summary>
    /// <param name="adId">Identificador do anúncio.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Anúncio localizado ou nulo.</returns>
    Task<Ad?> GetAdByIdAsync(
        Guid adId,
        CancellationToken cancellationToken = default);
}
