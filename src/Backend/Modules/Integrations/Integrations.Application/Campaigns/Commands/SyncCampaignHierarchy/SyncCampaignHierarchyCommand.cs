using BuildingBlocks.Application.Messaging;
using Integrations.Application.Campaigns.DTOs;

namespace Integrations.Application.Campaigns.Commands.SyncCampaignHierarchy;

/// <summary>
/// Comando que dispara a sincronização estrutural de campanhas, conjuntos e anúncios
/// para as contas conectadas de um determinado workspace.
/// </summary>
/// <param name="WorkspaceId">Identificador único do workspace da agência.</param>
/// <param name="ConnectedAdAccountId">Filtro opcional por conta conectada específica.</param>
/// <param name="Platform">Filtro opcional por plataforma de anúncios (MetaAds, GoogleAds, TikTokAds, BingAds).</param>
public sealed record SyncCampaignHierarchyCommand(
    Guid WorkspaceId,
    Guid? ConnectedAdAccountId = null,
    string? Platform = null) : ICommand<SyncCampaignHierarchySummaryDto>;
