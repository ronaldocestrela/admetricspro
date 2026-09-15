using BuildingBlocks.Domain.Abstractions;

namespace BuildingBlocks.Domain.Campaigns.Events;

/// <summary>
/// Evento de domínio emitido após a conclusão bem-sucedida da sincronização estrutural de campanhas
/// para uma conta de anúncios conectada ao workspace.
/// </summary>
/// <param name="TenantId">Identificador único do inquilino contextual.</param>
/// <param name="WorkspaceId">Identificador único do workspace da agência.</param>
/// <param name="ConnectedAdAccountId">Identificador único da conta conectada sincronizada.</param>
/// <param name="Platform">Nome da plataforma de anúncios (MetaAds, GoogleAds, TikTokAds, BingAds).</param>
/// <param name="CampaignsSynced">Quantidade total de campanhas inseridas ou atualizadas.</param>
/// <param name="AdSetsSynced">Quantidade total de conjuntos ou grupos inseridos ou atualizados.</param>
/// <param name="AdsSynced">Quantidade total de anúncios ou criativos inseridos ou atualizados.</param>
/// <param name="SyncedAtUtc">Carimbo de data/hora UTC em que a sincronização foi finalizada.</param>
public sealed record CampaignHierarchySyncedEvent(
    Guid TenantId,
    Guid WorkspaceId,
    Guid ConnectedAdAccountId,
    string Platform,
    int CampaignsSynced,
    int AdSetsSynced,
    int AdsSynced,
    DateTime SyncedAtUtc) : IDomainEvent;
