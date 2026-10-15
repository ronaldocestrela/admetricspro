using BuildingBlocks.Application.Campaigns.Commands;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Integrations.Application.Persistence;
using Integrations.Domain.Campaigns;
using Integrations.Application.Meta;
using MediatR;

namespace Integrations.Application.Campaigns.Commands.Mutations;

/// <summary>
/// Manipulador do comando in-memory para pausar uma campanha via módulo Integrations.
/// </summary>
public sealed class PauseCampaignCommandHandler : ICommandHandler<PauseCampaignCommand>
{
    private readonly ICampaignHierarchyRepository _hierarchyRepository;
    private readonly IIntegrationsUnitOfWork _unitOfWork;
    private readonly ISender _sender;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="PauseCampaignCommandHandler"/>.
    /// </summary>
    public PauseCampaignCommandHandler(
        ICampaignHierarchyRepository hierarchyRepository,
        IIntegrationsUnitOfWork unitOfWork, ISender sender)
    {
        _hierarchyRepository = hierarchyRepository ?? throw new ArgumentNullException(nameof(hierarchyRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _sender = sender;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(PauseCampaignCommand command, CancellationToken cancellationToken)
    {
        var campaign = await _hierarchyRepository.GetCampaignByIdAsync(command.CampaignId, cancellationToken);
        if (campaign is null)
        {
            return Result.Failure(Error.NotFound("Campaign.NotFound", "Campanha não localizada no banco do inquilino."));
        }

        if (campaign.WorkspaceId != command.WorkspaceId)
        {
            return Result.Failure(Error.Validation("Campaign.WorkspaceMismatch", "A campanha não pertence ao workspace informado."));
        }

        var external = await MetaCampaignMutationForwarder.ForwardAsync(campaign, _hierarchyRepository, _sender, cancellationToken, status: "PAUSED");
        if (external.IsFailure) return external;
        var pauseResult = campaign.Pause();
        if (pauseResult.IsFailure)
        {
            return pauseResult;
        }

        await _unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
