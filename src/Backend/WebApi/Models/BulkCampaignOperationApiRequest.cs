using BuildingBlocks.Application.Campaigns.Commands;

namespace WebApi.Models;

/// <summary>
/// Modelo de entrada para requisição de operações em lote em campanhas publicitárias.
/// </summary>
/// <param name="WorkspaceId">Identificador único do workspace associado.</param>
/// <param name="Operations">Coleção de operações a serem aplicadas no lote.</param>
public sealed record BulkCampaignOperationApiRequest(
    Guid WorkspaceId,
    IReadOnlyList<BulkCampaignOperationItemApiDto> Operations);

/// <summary>
/// DTO do item de operação em lote na camada de API.
/// </summary>
/// <param name="CampaignId">Identificador único da campanha no banco do inquilino.</param>
/// <param name="Action">Ação a ser executada (Activate, Pause, AdjustBudget).</param>
/// <param name="DailyBudget">Novo orçamento diário absoluto quando fixo.</param>
/// <param name="PercentageChange">Variação percentual aplicada ao orçamento.</param>
/// <param name="Reason">Justificativa da alteração.</param>
public sealed record BulkCampaignOperationItemApiDto(
    Guid CampaignId,
    BulkCampaignActionType Action,
    decimal? DailyBudget = null,
    decimal? PercentageChange = null,
    string? Reason = null);
