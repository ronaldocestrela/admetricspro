using BuildingBlocks.Application.Campaigns.Commands;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Integrations.Application.Persistence;
using Integrations.Domain.Campaigns;

namespace Integrations.Application.Campaigns.Commands.Mutations;

/// <summary>
/// Manipulador do comando in-memory para ativar uma campanha via módulo Integrations.
/// </summary>
public sealed class ActivateCampaignCommandHandler : ICommandHandler<ActivateCampaignCommand>
{
    private readonly ICampaignHierarchyRepository _hierarchyRepository;
    private readonly IIntegrationsUnitOfWork _unitOfWork;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ActivateCampaignCommandHandler"/>.
    /// </summary>
    /// <param name="hierarchyRepository">Repositório de persistência da hierarquia de campanhas.</param>
    /// <param name="unitOfWork">Unidade de trabalho do módulo de integrações.</param>
    public ActivateCampaignCommandHandler(
        ICampaignHierarchyRepository hierarchyRepository,
        IIntegrationsUnitOfWork unitOfWork)
    {
        _hierarchyRepository = hierarchyRepository ?? throw new ArgumentNullException(nameof(hierarchyRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <inheritdoc />
    public async Task<Result> Handle(ActivateCampaignCommand command, CancellationToken cancellationToken)
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

        var activateResult = campaign.Activate();
        if (activateResult.IsFailure)
        {
            return activateResult;
        }

        await _unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
