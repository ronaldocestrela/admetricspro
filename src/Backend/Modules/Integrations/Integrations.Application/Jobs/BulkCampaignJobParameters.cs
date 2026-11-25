using BuildingBlocks.Application.Campaigns.Commands;

namespace Integrations.Application.Jobs;

/// <summary>
/// Parâmetros persistidos para job de operação em massa.
/// </summary>
public sealed record BulkCampaignJobParameters(
    Guid WorkspaceId,
    IReadOnlyList<BulkCampaignOperationItem> Operations);