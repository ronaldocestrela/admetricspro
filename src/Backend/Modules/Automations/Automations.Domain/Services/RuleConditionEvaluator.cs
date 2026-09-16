using BuildingBlocks.Domain.Automations;

namespace Automations.Domain.Services;

/// <summary>
/// Implementação concreta do motor avaliador de predicados e árvores lógicas da DSL de regras.
/// Processamento em memória puro e desacoplado de dependências de rede e I/O.
/// </summary>
public sealed class RuleConditionEvaluator : IRuleConditionEvaluator
{
    /// <inheritdoc />
    public EvaluationOutcome Evaluate(IRuleCondition condition, RuleEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(context);

        var matchedEntityIds = new HashSet<Guid>();
        var details = new List<string>();

        var (isTriggered, predicateCount) = EvaluateNode(condition, context, matchedEntityIds, details);

        return new EvaluationOutcome(
            isTriggered,
            matchedEntityIds.ToList(),
            predicateCount,
            details);
    }

    private static (bool IsSuccess, int PredicatesEvaluated) EvaluateNode(
        IRuleCondition condition,
        RuleEvaluationContext context,
        HashSet<Guid> matchedEntities,
        List<string> details)
    {
        if (condition is MetricPredicate leaf)
        {
            return EvaluateMetricPredicate(leaf, context, matchedEntities, details);
        }

        if (condition is RuleConditionGroup group)
        {
            return EvaluateConditionGroup(group, context, matchedEntities, details);
        }

        details.Add($"Tipo de condição não reconhecido: {condition.GetType().Name}");
        return (false, 0);
    }

    private static (bool IsSuccess, int PredicatesEvaluated) EvaluateMetricPredicate(
        MetricPredicate leaf,
        RuleEvaluationContext context,
        HashSet<Guid> matchedEntities,
        List<string> details)
    {
        var matchingSnapshots = context.Snapshots.Where(s =>
            (leaf.Platform.Equals("All", StringComparison.OrdinalIgnoreCase) ||
             s.Platform.Equals(leaf.Platform, StringComparison.OrdinalIgnoreCase)) &&
            s.Scope == leaf.Scope &&
            s.TimeWindowHours == leaf.TimeWindowHours &&
            (leaf.TargetEntityId == null || s.EntityId == leaf.TargetEntityId)).ToList();

        if (matchingSnapshots.Count == 0)
        {
            details.Add($"Nenhuma métrica localizada para o predicado: {leaf.Description}");
            return (false, 1);
        }

        var anyMatched = false;
        foreach (var snapshot in matchingSnapshots)
        {
            var actualValue = snapshot.GetValueForMetric(leaf.Metric);
            var isComparisonSatisfied = EvaluateComparison(actualValue, leaf.Operator, leaf.Threshold);

            if (isComparisonSatisfied)
            {
                anyMatched = true;
                if (snapshot.EntityId.HasValue && snapshot.EntityId.Value != Guid.Empty)
                {
                    matchedEntities.Add(snapshot.EntityId.Value);
                }

                details.Add($"Gatilho satisfeito: {leaf.Description} (Valor real: {actualValue})");
            }
            else
            {
                details.Add($"Gatilho não satisfeito: {leaf.Description} (Valor real: {actualValue})");
            }
        }

        return (anyMatched, 1);
    }

    private static (bool IsSuccess, int PredicatesEvaluated) EvaluateConditionGroup(
        RuleConditionGroup group,
        RuleEvaluationContext context,
        HashSet<Guid> matchedEntities,
        List<string> details)
    {
        if (group.Conditions.Count == 0)
        {
            details.Add("Grupo de condições vazio.");
            return (false, 0);
        }

        var totalPredicates = 0;

        switch (group.LogicalOperator)
        {
            case LogicalOperator.And:
            {
                var allTrue = true;
                foreach (var child in group.Conditions)
                {
                    var (childSuccess, count) = EvaluateNode(child, context, matchedEntities, details);
                    totalPredicates += count;
                    if (!childSuccess)
                    {
                        allTrue = false;
                    }
                }
                return (allTrue, totalPredicates);
            }

            case LogicalOperator.Or:
            {
                var anyTrue = false;
                foreach (var child in group.Conditions)
                {
                    var (childSuccess, count) = EvaluateNode(child, context, matchedEntities, details);
                    totalPredicates += count;
                    if (childSuccess)
                    {
                        anyTrue = true;
                    }
                }
                return (anyTrue, totalPredicates);
            }

            case LogicalOperator.Not:
            {
                var firstChild = group.Conditions.First();
                var (childSuccess, count) = EvaluateNode(firstChild, context, matchedEntities, details);
                totalPredicates += count;
                return (!childSuccess, totalPredicates);
            }

            default:
                details.Add($"Operador lógico desconhecido: {group.LogicalOperator}");
                return (false, 0);
        }
    }

    private static bool EvaluateComparison(decimal actual, ComparisonOperator op, decimal threshold) => op switch
    {
        ComparisonOperator.GreaterThan => actual > threshold,
        ComparisonOperator.GreaterThanOrEqual => actual >= threshold,
        ComparisonOperator.LessThan => actual < threshold,
        ComparisonOperator.LessThanOrEqual => actual <= threshold,
        ComparisonOperator.Equal => actual == threshold,
        ComparisonOperator.NotEqual => actual != threshold,
        _ => false
    };
}
