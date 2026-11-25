using Analytics.Application.Taxonomy.Queries.BatchClassifyTaxonomy;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Integrations.Application.Campaigns.Queries.GetCampaignHierarchy;
using MediatR;

namespace Analytics.Application.Ingestion.Commands.ProcessCampaignHierarchySynced;

/// <summary>
/// Comando de reação analítica após sincronização durável da hierarquia (taxonomia em memória).
/// </summary>
/// <param name="WorkspaceId">Workspace sincronizado.</param>
/// <param name="ConnectedAdAccountId">Conta conectada.</param>
public sealed record ProcessCampaignHierarchySyncedCommand(
    Guid WorkspaceId,
    Guid ConnectedAdAccountId) : ICommand;

/// <summary>
/// Relê a hierarquia persistida e classifica nomenclaturas via motor de taxonomia.
/// </summary>
public sealed class ProcessCampaignHierarchySyncedCommandHandler : ICommandHandler<ProcessCampaignHierarchySyncedCommand>
{
    private readonly ISender _sender;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ProcessCampaignHierarchySyncedCommandHandler"/>.
    /// </summary>
    /// <param name="sender">Mediador para consultas cross-módulo.</param>
    public ProcessCampaignHierarchySyncedCommandHandler(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    /// <inheritdoc />
    public async Task<Result> Handle(ProcessCampaignHierarchySyncedCommand command, CancellationToken cancellationToken)
    {
        var hierarchy = await _sender.Send(
            new GetCampaignHierarchyQuery(command.WorkspaceId, command.ConnectedAdAccountId),
            cancellationToken);

        if (hierarchy.IsFailure)
        {
            return Result.Failure(hierarchy.Error);
        }

        var items = new List<BatchClassifyTaxonomyItem>();
        foreach (var campaign in hierarchy.Value)
        {
            foreach (var adSet in campaign.AdSets)
            {
                foreach (var ad in adSet.Ads)
                {
                    items.Add(new BatchClassifyTaxonomyItem(
                        campaign.Name,
                        adSet.Name,
                        ad.Name,
                        ad.Id.ToString()));
                }
            }

            if (campaign.AdSets.Count == 0)
            {
                items.Add(new BatchClassifyTaxonomyItem(campaign.Name, null, null, campaign.Id.ToString()));
            }
        }

        if (items.Count == 0)
        {
            return Result.Success();
        }

        var classify = await _sender.Send(new BatchClassifyTaxonomyQuery(items), cancellationToken);
        return classify.IsFailure ? Result.Failure(classify.Error) : Result.Success();
    }
}