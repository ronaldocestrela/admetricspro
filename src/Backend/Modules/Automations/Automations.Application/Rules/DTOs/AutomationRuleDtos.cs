using BuildingBlocks.Domain.Automations;

namespace Automations.Application.Rules.DTOs;

/// <summary>
/// DTO representativo da regra de automação cross-platform.
/// </summary>
public sealed record AutomationRuleDto(
    Guid Id,
    Guid WorkspaceId,
    string Name,
    string Description,
    bool IsEnabled,
    RuleConditionGroup ConditionTree,
    IReadOnlyCollection<RuleAction> Actions,
    DateTime? LastTriggeredAtUtc,
    int ExecutionCount,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

/// <summary>
/// Relatório de avaliação e execução sob demanda ou programada de uma regra.
/// </summary>
public sealed record RuleEvaluationReportDto(
    Guid RuleId,
    string RuleName,
    bool IsTriggered,
    int MatchedEntitiesCount,
    int ActionsDispatchedCount,
    IReadOnlyList<string> Details);
