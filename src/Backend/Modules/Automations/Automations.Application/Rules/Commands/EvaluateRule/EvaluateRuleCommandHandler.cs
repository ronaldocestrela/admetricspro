using Automations.Application.Persistence;
using Automations.Application.Rules.DTOs;
using Automations.Application.Rules.Services;
using Automations.Domain.Rules;
using Automations.Domain.Services;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;

namespace Automations.Application.Rules.Commands.EvaluateRule;

/// <summary>
/// Comando para avaliar e executar uma regra de automação programada ou sob demanda.
/// </summary>
public sealed record EvaluateRuleCommand(
    Guid WorkspaceId,
    Guid RuleId) : ICommand<RuleEvaluationReportDto>;

/// <summary>
/// Manipulador que orquestra a avaliação da regra e o disparo de mutações.
/// </summary>
public sealed class EvaluateRuleCommandHandler : ICommandHandler<EvaluateRuleCommand, RuleEvaluationReportDto>
{
    private readonly IAutomationRuleRepository _ruleRepository;
    private readonly IAutomationsMetricsProvider _metricsProvider;
    private readonly IRuleConditionEvaluator _conditionEvaluator;
    private readonly IRuleActionDispatcher _actionDispatcher;
    private readonly IAutomationsUnitOfWork _unitOfWork;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="EvaluateRuleCommandHandler"/>.
    /// </summary>
    public EvaluateRuleCommandHandler(
        IAutomationRuleRepository ruleRepository,
        IAutomationsMetricsProvider metricsProvider,
        IRuleConditionEvaluator conditionEvaluator,
        IRuleActionDispatcher actionDispatcher,
        IAutomationsUnitOfWork unitOfWork)
    {
        _ruleRepository = ruleRepository ?? throw new ArgumentNullException(nameof(ruleRepository));
        _metricsProvider = metricsProvider ?? throw new ArgumentNullException(nameof(metricsProvider));
        _conditionEvaluator = conditionEvaluator ?? throw new ArgumentNullException(nameof(conditionEvaluator));
        _actionDispatcher = actionDispatcher ?? throw new ArgumentNullException(nameof(actionDispatcher));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <inheritdoc />
    public async Task<Result<RuleEvaluationReportDto>> Handle(EvaluateRuleCommand command, CancellationToken cancellationToken)
    {
        var rule = await _ruleRepository.GetByIdAsync(command.RuleId, cancellationToken);
        if (rule is null)
        {
            return Result<RuleEvaluationReportDto>.Failure(
                Error.NotFound("AutomationRule.NotFound", "Regra de automação não localizada."));
        }

        if (rule.WorkspaceId != command.WorkspaceId)
        {
            return Result<RuleEvaluationReportDto>.Failure(
                Error.Validation("AutomationRule.WorkspaceMismatch", "A regra não pertence ao workspace informado."));
        }

        if (!rule.IsEnabled)
        {
            return Result<RuleEvaluationReportDto>.Success(new RuleEvaluationReportDto(
                rule.Id,
                rule.Name,
                false,
                0,
                0,
                new[] { "A regra está inativa." }));
        }

        var context = await _metricsProvider.LoadMetricsContextAsync(command.WorkspaceId, cancellationToken);
        var evaluationOutcome = _conditionEvaluator.Evaluate(rule.ConditionTree, context);

        var actionsDispatched = 0;
        if (evaluationOutcome.IsTriggered)
        {
            var dispatchResult = await _actionDispatcher.DispatchActionsAsync(
                command.WorkspaceId,
                rule.Actions,
                evaluationOutcome.MatchedEntityIds,
                cancellationToken);

            if (dispatchResult.IsSuccess)
            {
                actionsDispatched = dispatchResult.Value;
            }

            rule.RecordTriggered(DateTime.UtcNow);
            await _unitOfWork.CommitAsync(cancellationToken);
        }

        var report = new RuleEvaluationReportDto(
            rule.Id,
            rule.Name,
            evaluationOutcome.IsTriggered,
            evaluationOutcome.MatchedEntityIds.Count,
            actionsDispatched,
            evaluationOutcome.Details);

        return Result<RuleEvaluationReportDto>.Success(report);
    }
}
