using BuildingBlocks.Domain.Abstractions;

namespace BuildingBlocks.Domain.Campaigns.Events;

/// <summary>
/// Evento de domínio in-memory emitido após a conclusão bem-sucedida do pipeline de ingestão de métricas
/// analíticas de campanhas para uma conta conectada ao workspace.
/// </summary>
/// <param name="TenantId">Identificador único do inquilino contextual.</param>
/// <param name="WorkspaceId">Identificador único do workspace da agência.</param>
/// <param name="ConnectedAdAccountId">Identificador único da conta conectada sincronizada.</param>
/// <param name="Platform">Nome da plataforma de anúncios (MetaAds, GoogleAds, TikTokAds, BingAds).</param>
/// <param name="TotalRecordsIngested">Quantidade total de registros diários ou horários processados de forma idempotente.</param>
/// <param name="Granularity">Granularidade temporal consolidada (Daily ou Hourly).</param>
/// <param name="TotalSpend">Valor financeiro total investido consolidado no lote.</param>
/// <param name="SyncedAtUtc">Carimbo de data/hora UTC em que a sincronização foi finalizada.</param>
public sealed record CampaignMetricsSyncedEvent(
    Guid TenantId,
    Guid WorkspaceId,
    Guid ConnectedAdAccountId,
    string Platform,
    int TotalRecordsIngested,
    MetricGranularity Granularity,
    decimal TotalSpend,
    DateTime SyncedAtUtc) : IDomainEvent;
