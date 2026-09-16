using BuildingBlocks.Application.Campaigns.Commands;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Integrations.Application.Persistence;
using Integrations.Domain.Campaigns;

namespace Integrations.Application.Campaigns.Commands.Mutations;

/// <summary>
/// Manipulador do comando in-memory para ajustar o orçamento diário de uma campanha.
/// </summary>
public sealed class AdjustCampaignBudgetCommandHandler : ICommandHandler<AdjustCampaignBudgetCommand>
{
    private readonly ICampaignHierarchyRepository _hierarchyRepository;
    private readonly IIntegrationsUnitOfWork _unitOfWork;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="AdjustCampaignBudgetCommandHandler"/>.
    /// </summary>
    public AdjustCampaignBudgetCommandHandler(
        ICampaignHierarchyRepository hierarchyRepository,
        IIntegrationsUnitOfWork unitOfWork)
    {
        _hierarchyRepository = hierarchyRepository ?? throw new ArgumentNullException(nameof(hierarchyRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <inheritdoc />
    public async Task<Result> Handle(AdjustCampaignBudgetCommand command, CancellationToken cancellationToken)
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

        decimal newBudget;
        if (command.DailyBudget.HasValue)
        {
            newBudget = command.DailyBudget.Value;
        }
        else if (command.PercentageChange.HasValue)
        {
            var currentBudget = campaign.DailyBudget ?? 0m;
            if (currentBudget <= 0)
            {
                return Result.Failure(Error.Validation("Campaign.CurrentBudgetZero", "Não é possível aplicar ajuste percentual em campanha com orçamento zerado ou não configurado."));
            }

            var multiplier = 1m + (command.PercentageChange.Value / 100m);
            newBudget = Math.Max(0m, Math.Round(currentBudget * multiplier, 2));
        }
        else
        {
            return Result.Failure(Error.Validation("Campaign.NoBudgetSpecified", "Informe um orçamento fixo ou um percentual de variação."));
        }

        var updateResult = campaign.UpdateDailyBudget(newBudget);
        if (updateResult.IsFailure)
        {
            return updateResult;
        }

        await _unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
