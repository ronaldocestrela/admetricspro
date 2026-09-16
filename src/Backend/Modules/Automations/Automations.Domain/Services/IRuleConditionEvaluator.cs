using BuildingBlocks.Domain.Automations;

namespace Automations.Domain.Services;

/// <summary>
/// Resultado da avaliação de uma árvore de condições da DSL de automação.
/// </summary>
public sealed class EvaluationOutcome
{
    /// <summary>
    /// Inicializa uma nova instância de <see cref="EvaluationOutcome"/>.
    /// </summary>
    public EvaluationOutcome(
        bool isTriggered,
        IReadOnlyList<Guid> matchedEntityIds,
        int evaluatedPredicatesCount,
        IReadOnlyList<string> details)
    {
        IsTriggered = isTriggered;
        MatchedEntityIds = matchedEntityIds;
        EvaluatedPredicatesCount = evaluatedPredicatesCount;
        Details = details;
    }

    /// <summary>
    /// Indica se todos os critérios da árvore de condições foram satisfeitos.
    /// </summary>
    public bool IsTriggered { get; }

    /// <summary>
    /// Lista de identificadores de entidades (campanhas, anúncios) que atenderam as condições.
    /// </summary>
    public IReadOnlyList<Guid> MatchedEntityIds { get; }

    /// <summary>
    /// Quantidade total de predicados folha avaliados.
    /// </summary>
    public int EvaluatedPredicatesCount { get; }

    /// <summary>
    /// Mensagens de diagnóstico e rastreabilidade da avaliação.
    /// </summary>
    public IReadOnlyList<string> Details { get; }

    /// <summary>
    /// Cria um resultado com gatilho não disparado.
    /// </summary>
    public static EvaluationOutcome NotTriggered(string reason, int count = 0) =>
        new(false, Array.Empty<Guid>(), count, new[] { reason });
}

/// <summary>
/// Contrato do motor de avaliação de árvores de condições da DSL de automações.
/// </summary>
public interface IRuleConditionEvaluator
{
    /// <summary>
    /// Avalia uma árvore de predicados contra o contexto de métricas em memória.
    /// </summary>
    /// <param name="condition">Nó raiz da condição ou grupo lógico.</param>
    /// <param name="context">Contexto de dados de desempenho em memória.</param>
    /// <returns>Resultado com indicação se o gatilho foi disparado e detalhes da avaliação.</returns>
    EvaluationOutcome Evaluate(IRuleCondition condition, RuleEvaluationContext context);
}
