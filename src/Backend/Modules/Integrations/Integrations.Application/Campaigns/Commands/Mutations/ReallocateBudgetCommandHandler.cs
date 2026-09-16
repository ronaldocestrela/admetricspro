using BuildingBlocks.Application.Campaigns.Commands;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Integrations.Application.Persistence;
using Integrations.Domain.Campaigns;

namespace Integrations.Application.Campaigns.Commands.Mutations;

/// <summary>
/// Manipulador do comando in-memory para transferir e realocar saldo entre campanhas.
/// </summary>
public sealed class ReallocateBudgetCommandHandler : ICommandHandler<ReallocateBudgetCommand>
{
    private readonly ICampaignHierarchyRepository _hierarchyRepository;
    private readonly IIntegrationsUnitOfWork _unitOfWork;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ReallocateBudgetCommandHandler"/>.
    /// </summary>
    public ReallocateBudgetCommandHandler(
        ICampaignHierarchyRepository hierarchyRepository,
        IIntegrationsUnitOfWork unitOfWork)
    {
        _hierarchyRepository = hierarchyRepository ?? throw new ArgumentNullException(nameof(hierarchyRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <inheritdoc />
    public async Task<Result> Handle(ReallocateBudgetCommand command, CancellationToken cancellationToken)
    {
        if (command.SourceCampaignId == command.TargetCampaignId)
        {
            return Result.Failure(Error.Validation("Reallocation.SameCampaign", "A campanha de origem e destino não podem ser a mesma."));
        }

        var sourceCampaign = await _hierarchyRepository.GetCampaignByIdAsync(command.SourceCampaignId, cancellationToken);
        if (sourceCampaign is null)
        {
            return Result.Failure(Error.NotFound("Campaign.SourceNotFound", "Campanha de origem não localizada."));
        }

        var targetCampaign = await _hierarchyRepository.GetCampaignByIdAsync(command.TargetCampaignId, cancellationToken);
        if (targetCampaign is null)
        {
            return Result.Failure(Error.NotFound("Campaign.TargetNotFound", "Campanha de destino não localizada."));
        }

        if (sourceCampaign.WorkspaceId != command.WorkspaceId || targetCampaign.WorkspaceId != command.WorkspaceId)
        {
            return Result.Failure(Error.Validation("Campaign.WorkspaceMismatch", "Ambas as campanhas devem pertencer ao mesmo workspace."));
        }

        var sourceBudget = sourceCampaign.DailyBudget ?? 0m;
        if (sourceBudget <= 0m)
        {
            return Result.Failure(Error.Validation("Campaign.SourceBudgetZero", "A campanha de origem possui orçamento zerado."));
        }

        decimal transferAmount;
        if (command.IsPercentage)
        {
            if (command.AmountOrPercentage <= 0m || command.AmountOrPercentage > 100m)
            {
                return Result.Failure(Error.Validation("Reallocation.InvalidPercentage", "O percentual de realocação deve estar entre 1% e 100%."));
            }

            transferAmount = Math.Round(sourceBudget * (command.AmountOrPercentage / 100m), 2);
        }
        else
        {
            if (command.AmountOrPercentage <= 0m)
            {
                return Result.Failure(Error.Validation("Reallocation.InvalidAmount", "O valor de transferência deve ser maior que zero."));
            }

            transferAmount = Math.Round(command.AmountOrPercentage, 2);
        }

        if (transferAmount > sourceBudget)
        {
            return Result.Failure(Error.Validation("Reallocation.InsufficientBudget", "O valor de transferência supera o orçamento disponível na campanha de origem."));
        }

        var newSourceBudget = sourceBudget - transferAmount;
        var targetBudget = targetCampaign.DailyBudget ?? 0m;
        var newTargetBudget = targetBudget + transferAmount;

        sourceCampaign.UpdateDailyBudget(newSourceBudget);
        targetCampaign.UpdateDailyBudget(newTargetBudget);

        await _unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
