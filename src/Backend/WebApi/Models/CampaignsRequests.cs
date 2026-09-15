namespace WebApi.Models;

/// <summary>
/// Modelo de requisição para disparar a sincronização estrutural de campanhas de um workspace.
/// </summary>
/// <param name="WorkspaceId">Identificador único do workspace.</param>
/// <param name="ConnectedAdAccountId">Filtro opcional por conta conectada específica.</param>
/// <param name="Platform">Filtro opcional por plataforma (MetaAds, GoogleAds, TikTokAds, BingAds).</param>
public sealed record SyncCampaignHierarchyApiRequest(
    Guid WorkspaceId,
    Guid? ConnectedAdAccountId = null,
    string? Platform = null);
