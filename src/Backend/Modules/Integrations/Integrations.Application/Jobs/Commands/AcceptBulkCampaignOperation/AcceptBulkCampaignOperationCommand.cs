using BuildingBlocks.Application.Campaigns.Commands;
using BuildingBlocks.Application.Messaging;

namespace Integrations.Application.Jobs.Commands.AcceptBulkCampaignOperation;

/// <summary>
/// Aceita operação em massa com execução assíncrona no consumidor.
/// </summary>
public sealed record AcceptBulkCampaignOperationCommand(
    Guid WorkspaceId,
    IReadOnlyList<BulkCampaignOperationItem> Operations) : ICommand<UserInitiatedJobDto>;