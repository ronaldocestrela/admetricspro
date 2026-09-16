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

/// <summary>
/// Modelo de requisição para disparar a ingestão de métricas analíticas diárias e horárias.
/// </summary>
/// <param name="WorkspaceId">Identificador obrigatório do workspace.</param>
/// <param name="ConnectedAdAccountId">Filtro opcional por conta conectada.</param>
/// <param name="Platform">Filtro opcional por plataforma de anúncios.</param>
/// <param name="StartDateUtc">Data inicial da janela de métricas (padrão: 7 dias atrás).</param>
/// <param name="EndDateUtc">Data final da janela de métricas (padrão: data atual em UTC).</param>
/// <param name="Granularity">Granularidade temporal ("Daily" ou "Hourly", padrão: "Daily").</param>
public sealed record SyncCampaignMetricsApiRequest(
    Guid WorkspaceId,
    Guid? ConnectedAdAccountId = null,
    string? Platform = null,
    DateTime? StartDateUtc = null,
    DateTime? EndDateUtc = null,
    string? Granularity = "Daily");
