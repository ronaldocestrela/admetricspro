using Automations.Application.Pacing.Queries.GetWorkspaceBudgetPacing;
using Automations.Application.Rules.Commands.EvaluateRule;
using Automations.Application.Rules.Queries.ListRulesByWorkspace;
using Automations.Application.SafetyGuards.Commands.CheckOverspending;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using MediatR;

namespace Automations.Application.Ingestion.Commands.ReactToCampaignMetricsSynced;

/// <summary>
/// Comando disparado após ingestão durável de métricas de campanha para avaliar regras, pacing e travas.
/// </summary>
/// <param name="WorkspaceId">Workspace afetado pela sincronização.</param>
public sealed record ReactToCampaignMetricsSyncedCommand(Guid WorkspaceId) : ICommand;

/// <summary>
/// Orquestra avaliação de regras habilitadas, trava de overspending e consulta de pacing após novas métricas.
/// </summary>
public sealed class ReactToCampaignMetricsSyncedCommandHandler : ICommandHandler<ReactToCampaignMetricsSyncedCommand>
{
    private readonly ISender _sender;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ReactToCampaignMetricsSyncedCommandHandler"/>.
    /// </summary>
    /// <param name="sender">Mediador in-process para comandos e consultas existentes.</param>
    public ReactToCampaignMetricsSyncedCommandHandler(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    /// <inheritdoc />
    public async Task<Result> Handle(ReactToCampaignMetricsSyncedCommand command, CancellationToken cancellationToken)
    {
        if (command.WorkspaceId == Guid.Empty)
        {
            return Result.Failure(
                Error.Validation("IngestionReaction.EmptyWorkspaceId", "O workspace é obrigatório para reação pós-ingestão."));
        }

        var rulesResult = await _sender.Send(new ListRulesByWorkspaceQuery(command.WorkspaceId), cancellationToken);
        if (rulesResult.IsFailure)
        {
            return Result.Failure(rulesResult.Error);
        }

        foreach (var rule in rulesResult.Value.Where(r => r.IsEnabled))
        {
            var evaluation = await _sender.Send(
                new EvaluateRuleCommand(command.WorkspaceId, rule.Id),
                cancellationToken);

            if (evaluation.IsFailure)
            {
                return Result.Failure(evaluation.Error);
            }
        }

        var overspending = await _sender.Send(new CheckOverspendingCommand(command.WorkspaceId), cancellationToken);
        if (overspending.IsFailure)
        {
            return Result.Failure(overspending.Error);
        }

        var now = DateTime.UtcNow;
        var pacing = await _sender.Send(
            new GetWorkspaceBudgetPacingQuery(command.WorkspaceId, now.Year, now.Month),
            cancellationToken);

        return pacing.IsFailure ? Result.Failure(pacing.Error) : Result.Success();
    }
}