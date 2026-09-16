using BuildingBlocks.Application.Campaigns.Commands;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Integrations.Application.Persistence;
using Integrations.Domain.Campaigns;

namespace Integrations.Application.Campaigns.Commands.Mutations;

/// <summary>
/// Manipulador do comando in-memory para orquestrar operações em lote com tolerância a falhas parciais (Subfase 5.1.2).
/// Suporta ativação, pausa e reajuste orçamentário em múltiplas campanhas de diferentes plataformas simultaneamente.
/// </summary>
public sealed class BulkCampaignOperationCommandHandler : ICommandHandler<BulkCampaignOperationCommand, BulkCampaignOperationResultDto>
{
    private const int MaxBatchSize = 100;
    private readonly ICampaignHierarchyRepository _hierarchyRepository;
    private readonly IIntegrationsUnitOfWork _unitOfWork;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="BulkCampaignOperationCommandHandler"/>.
    /// </summary>
    /// <param name="hierarchyRepository">Repositório de persistência da hierarquia de campanhas.</param>
    /// <param name="unitOfWork">Unidade de trabalho do módulo de integrações.</param>
    public BulkCampaignOperationCommandHandler(
        ICampaignHierarchyRepository hierarchyRepository,
        IIntegrationsUnitOfWork unitOfWork)
    {
        _hierarchyRepository = hierarchyRepository ?? throw new ArgumentNullException(nameof(hierarchyRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <inheritdoc />
    public async Task<Result<BulkCampaignOperationResultDto>> Handle(
        BulkCampaignOperationCommand command,
        CancellationToken cancellationToken)
    {
        if (command.WorkspaceId == Guid.Empty)
        {
            return Result<BulkCampaignOperationResultDto>.Failure(
                Error.Validation("BulkCampaign.InvalidWorkspaceId", "O identificador do workspace é obrigatório."));
        }

        if (command.Operations is null || command.Operations.Count == 0)
        {
            return Result<BulkCampaignOperationResultDto>.Failure(
                Error.Validation("BulkCampaign.EmptyList", "A lista de operações em lote não pode ser vazia."));
        }

        if (command.Operations.Count > MaxBatchSize)
        {
            return Result<BulkCampaignOperationResultDto>.Failure(
                Error.Validation("BulkCampaign.BatchLimitExceeded", $"O lote excede o limite máximo permitido de {MaxBatchSize} operações simultâneas."));
        }

        var succeededItems = new List<BulkOperationSuccessItemDto>();
        var failedItems = new List<BulkOperationFailureItemDto>();

        foreach (var op in command.Operations)
        {
            if (op.CampaignId == Guid.Empty)
            {
                failedItems.Add(new BulkOperationFailureItemDto(
                    op.CampaignId,
                    op.Action,
                    "Campaign.InvalidId",
                    "O identificador da campanha é inválido."));
                continue;
            }

            var campaign = await _hierarchyRepository.GetCampaignByIdAsync(op.CampaignId, cancellationToken);
            if (campaign is null)
            {
                failedItems.Add(new BulkOperationFailureItemDto(
                    op.CampaignId,
                    op.Action,
                    "Campaign.NotFound",
                    "Campanha não localizada no banco do inquilino."));
                continue;
            }

            if (campaign.WorkspaceId != command.WorkspaceId)
            {
                failedItems.Add(new BulkOperationFailureItemDto(
                    op.CampaignId,
                    op.Action,
                    "Campaign.WorkspaceMismatch",
                    "A campanha não pertence ao workspace informado."));
                continue;
            }

            var prevStatus = campaign.Status;
            var prevBudget = campaign.DailyBudget;

            switch (op.Action)
            {
                case BulkCampaignActionType.Activate:
                {
                    var actRes = campaign.Activate();
                    if (actRes.IsFailure)
                    {
                        failedItems.Add(new BulkOperationFailureItemDto(
                            op.CampaignId,
                            op.Action,
                            actRes.Error.Code,
                            actRes.Error.Description));
                    }
                    else
                    {
                        succeededItems.Add(new BulkOperationSuccessItemDto(
                            campaign.Id,
                            campaign.Name,
                            campaign.Platform,
                            op.Action,
                            prevStatus,
                            campaign.Status,
                            prevBudget,
                            campaign.DailyBudget));
                    }
                    break;
                }

                case BulkCampaignActionType.Pause:
                {
                    var pauseRes = campaign.Pause();
                    if (pauseRes.IsFailure)
                    {
                        failedItems.Add(new BulkOperationFailureItemDto(
                            op.CampaignId,
                            op.Action,
                            pauseRes.Error.Code,
                            pauseRes.Error.Description));
                    }
                    else
                    {
                        succeededItems.Add(new BulkOperationSuccessItemDto(
                            campaign.Id,
                            campaign.Name,
                            campaign.Platform,
                            op.Action,
                            prevStatus,
                            campaign.Status,
                            prevBudget,
                            campaign.DailyBudget));
                    }
                    break;
                }

                case BulkCampaignActionType.AdjustBudget:
                {
                    decimal newBudget;
                    if (op.DailyBudget.HasValue)
                    {
                        newBudget = op.DailyBudget.Value;
                    }
                    else if (op.PercentageChange.HasValue)
                    {
                        var currentBudget = campaign.DailyBudget ?? 0m;
                        if (currentBudget <= 0)
                        {
                            failedItems.Add(new BulkOperationFailureItemDto(
                                op.CampaignId,
                                op.Action,
                                "Campaign.CurrentBudgetZero",
                                "Não é possível aplicar ajuste percentual em campanha com orçamento zerado ou não configurado."));
                            continue;
                        }

                        var multiplier = 1m + (op.PercentageChange.Value / 100m);
                        newBudget = Math.Max(0m, Math.Round(currentBudget * multiplier, 2));
                    }
                    else
                    {
                        failedItems.Add(new BulkOperationFailureItemDto(
                            op.CampaignId,
                            op.Action,
                            "Campaign.NoBudgetSpecified",
                            "Informe um orçamento fixo ou um percentual de variação."));
                        continue;
                    }

                    var budgetRes = campaign.UpdateDailyBudget(newBudget);
                    if (budgetRes.IsFailure)
                    {
                        failedItems.Add(new BulkOperationFailureItemDto(
                            op.CampaignId,
                            op.Action,
                            budgetRes.Error.Code,
                            budgetRes.Error.Description));
                    }
                    else
                    {
                        succeededItems.Add(new BulkOperationSuccessItemDto(
                            campaign.Id,
                            campaign.Name,
                            campaign.Platform,
                            op.Action,
                            prevStatus,
                            campaign.Status,
                            prevBudget,
                            campaign.DailyBudget));
                    }
                    break;
                }

                default:
                {
                    failedItems.Add(new BulkOperationFailureItemDto(
                        op.CampaignId,
                        op.Action,
                        "Campaign.UnsupportedAction",
                        $"Ação '{op.Action}' não é suportada."));
                    break;
                }
            }
        }

        if (succeededItems.Count > 0)
        {
            await _unitOfWork.CommitAsync(cancellationToken);
        }

        var resultDto = new BulkCampaignOperationResultDto
        {
            TotalRequested = command.Operations.Count,
            TotalSucceeded = succeededItems.Count,
            TotalFailed = failedItems.Count,
            SucceededItems = succeededItems,
            FailedItems = failedItems
        };

        return Result<BulkCampaignOperationResultDto>.Success(resultDto);
    }
}
