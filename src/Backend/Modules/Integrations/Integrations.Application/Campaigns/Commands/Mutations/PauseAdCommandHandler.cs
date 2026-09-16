using BuildingBlocks.Application.Campaigns.Commands;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Integrations.Application.Persistence;
using Integrations.Domain.Campaigns;

namespace Integrations.Application.Campaigns.Commands.Mutations;

/// <summary>
/// Manipulador do comando in-memory para pausar um anúncio / criativo via módulo Integrations.
/// </summary>
public sealed class PauseAdCommandHandler : ICommandHandler<PauseAdCommand>
{
    private readonly ICampaignHierarchyRepository _hierarchyRepository;
    private readonly IIntegrationsUnitOfWork _unitOfWork;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="PauseAdCommandHandler"/>.
    /// </summary>
    public PauseAdCommandHandler(
        ICampaignHierarchyRepository hierarchyRepository,
        IIntegrationsUnitOfWork unitOfWork)
    {
        _hierarchyRepository = hierarchyRepository ?? throw new ArgumentNullException(nameof(hierarchyRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <inheritdoc />
    public async Task<Result> Handle(PauseAdCommand command, CancellationToken cancellationToken)
    {
        var ad = await _hierarchyRepository.GetAdByIdAsync(command.AdId, cancellationToken);
        if (ad is null)
        {
            return Result.Failure(Error.NotFound("Ad.NotFound", "Anúncio não localizado no banco do inquilino."));
        }

        var pauseResult = ad.Pause();
        if (pauseResult.IsFailure)
        {
            return pauseResult;
        }

        await _unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
