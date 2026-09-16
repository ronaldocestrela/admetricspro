using BuildingBlocks.Domain.Automations;
using BuildingBlocks.Application.Campaigns.Commands;
using BuildingBlocks.Domain.Primitives;
using MediatR;

namespace Automations.Application.Rules.Services;

/// <summary>
/// Implementação concreta do despachador de ações de automação.
/// Converte as ações declarativas da DSL em comandos do MediatR enviados ao módulo Integrations.
/// </summary>
public sealed class RuleActionDispatcher : IRuleActionDispatcher
{
    private readonly ISender _sender;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="RuleActionDispatcher"/>.
    /// </summary>
    /// <param name="sender">Mediador in-memory MediatR.</param>
    public RuleActionDispatcher(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    /// <inheritdoc />
    public async Task<Result<int>> DispatchActionsAsync(
        Guid workspaceId,
        IEnumerable<RuleAction> actions,
        IReadOnlyList<Guid> matchedEntityIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actions);

        var executedCount = 0;

        foreach (var action in actions)
        {
            switch (action.Type)
            {
                case RuleActionType.PauseCampaign:
                {
                    var targets = action.TargetEntityId.HasValue
                        ? new[] { action.TargetEntityId.Value }
                        : matchedEntityIds;

                    foreach (var targetId in targets)
                    {
                        var command = new PauseCampaignCommand(workspaceId, targetId, "Regra de automação disparada");
                        var result = await _sender.Send(command, cancellationToken);
                        if (result.IsSuccess)
                        {
                            executedCount++;
                        }
                    }
                    break;
                }

                case RuleActionType.PauseAd:
                {
                    var targets = action.TargetEntityId.HasValue
                        ? new[] { action.TargetEntityId.Value }
                        : matchedEntityIds;

                    foreach (var targetId in targets)
                    {
                        var command = new PauseAdCommand(workspaceId, targetId, "Regra de automação disparada");
                        var result = await _sender.Send(command, cancellationToken);
                        if (result.IsSuccess)
                        {
                            executedCount++;
                        }
                    }
                    break;
                }

                case RuleActionType.AdjustBudgetPercentage:
                {
                    var targets = action.TargetEntityId.HasValue
                        ? new[] { action.TargetEntityId.Value }
                        : matchedEntityIds;

                    foreach (var targetId in targets)
                    {
                        var command = new AdjustCampaignBudgetCommand(
                            workspaceId,
                            targetId,
                            PercentageChange: action.Value);

                        var result = await _sender.Send(command, cancellationToken);
                        if (result.IsSuccess)
                        {
                            executedCount++;
                        }
                    }
                    break;
                }

                case RuleActionType.AdjustBudgetFixed:
                {
                    if (action.TargetEntityId.HasValue && action.Value.HasValue)
                    {
                        var command = new AdjustCampaignBudgetCommand(
                            workspaceId,
                            action.TargetEntityId.Value,
                            DailyBudget: action.Value.Value);

                        var result = await _sender.Send(command, cancellationToken);
                        if (result.IsSuccess)
                        {
                            executedCount++;
                        }
                    }
                    break;
                }

                case RuleActionType.ReallocateBudget:
                {
                    if (action.TargetEntityId.HasValue &&
                        action.DestinationEntityId.HasValue &&
                        action.Value.HasValue)
                    {
                        var command = new ReallocateBudgetCommand(
                            workspaceId,
                            action.TargetEntityId.Value,
                            action.DestinationEntityId.Value,
                            action.Value.Value,
                            IsPercentage: true);

                        var result = await _sender.Send(command, cancellationToken);
                        if (result.IsSuccess)
                        {
                            executedCount++;
                        }
                    }
                    break;
                }
            }
        }

        return Result<int>.Success(executedCount);
    }
}
