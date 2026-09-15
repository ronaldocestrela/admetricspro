using BuildingBlocks.Application.Messaging;
using Integrations.Application.Campaigns.DTOs;

namespace Integrations.Application.Campaigns.Queries.GetCampaignHierarchy;

/// <summary>
/// Consulta para obtenção da hierarquia completa de campanhas, conjuntos e anúncios de um workspace.
/// </summary>
/// <param name="WorkspaceId">Identificador do workspace da agência.</param>
/// <param name="ConnectedAdAccountId">Filtro opcional por conta conectada.</param>
/// <param name="CampaignId">Filtro opcional por campanha específica.</param>
/// <param name="Platform">Filtro opcional por plataforma.</param>
/// <param name="Status">Filtro opcional por status de campanha.</param>
public sealed record GetCampaignHierarchyQuery(
    Guid WorkspaceId,
    Guid? ConnectedAdAccountId = null,
    Guid? CampaignId = null,
    string? Platform = null,
    string? Status = null) : IQuery<IReadOnlyList<CampaignHierarchyDto>>;
