using System.Text.Json.Serialization;

namespace BuildingBlocks.Domain.Automations;

/// <summary>
/// Contrato base para nós da árvore de predicados da DSL de automação.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(MetricPredicate), "metric")]
[JsonDerivedType(typeof(RuleConditionGroup), "group")]
public interface IRuleCondition
{
    /// <summary>
    /// Obtém a descrição legível do predicado ou grupo.
    /// </summary>
    string Description { get; }
}

/// <summary>
/// Nó folha da DSL de regras que avalia uma métrica contra um limiar numérico.
/// </summary>
public sealed class MetricPredicate : IRuleCondition
{
    /// <summary>
    /// Inicializa uma nova instância de <see cref="MetricPredicate"/>.
    /// </summary>
    public MetricPredicate(
        string platform,
        RuleScope scope,
        MetricType metric,
        ComparisonOperator @operator,
        decimal threshold,
        int timeWindowHours,
        Guid? targetEntityId = null)
    {
        Platform = string.IsNullOrWhiteSpace(platform) ? "All" : platform.Trim();
        Scope = scope;
        Metric = metric;
        Operator = @operator;
        Threshold = threshold;
        TimeWindowHours = timeWindowHours > 0 ? timeWindowHours : 24;
        TargetEntityId = targetEntityId;
    }

    /// <summary>
    /// Construtor sem parâmetros para serialização.
    /// </summary>
    public MetricPredicate()
    {
        Platform = "All";
        Scope = RuleScope.Workspace;
        Metric = MetricType.Cpa;
        Operator = ComparisonOperator.GreaterThan;
        Threshold = 0m;
        TimeWindowHours = 24;
    }

    /// <summary>
    /// Plataforma alvo (MetaAds, GoogleAds, TikTokAds, BingAds, All).
    /// </summary>
    public string Platform { get; init; }

    /// <summary>
    /// Escopo do predicado (Workspace, Campaign, AdSet, Ad).
    /// </summary>
    public RuleScope Scope { get; init; }

    /// <summary>
    /// Métrica analisada.
    /// </summary>
    public MetricType Metric { get; init; }

    /// <summary>
    /// Operador relacional de comparação.
    /// </summary>
    public ComparisonOperator Operator { get; init; }

    /// <summary>
    /// Limiar numérico para o disparo.
    /// </summary>
    public decimal Threshold { get; init; }

    /// <summary>
    /// Janela temporal em horas considerada na agregação das métricas.
    /// </summary>
    public int TimeWindowHours { get; init; }

    /// <summary>
    /// Identificador opcional da entidade alvo específica.
    /// </summary>
    public Guid? TargetEntityId { get; init; }

    /// <inheritdoc />
    public string Description =>
        $"{Platform}.{Metric}[{TimeWindowHours}h] {OperatorToString(Operator)} {Threshold}";

    private static string OperatorToString(ComparisonOperator op) => op switch
    {
        ComparisonOperator.GreaterThan => ">",
        ComparisonOperator.GreaterThanOrEqual => ">=",
        ComparisonOperator.LessThan => "<",
        ComparisonOperator.LessThanOrEqual => "<=",
        ComparisonOperator.Equal => "==",
        ComparisonOperator.NotEqual => "!=",
        _ => "?"
    };
}

/// <summary>
/// Nó composto da árvore de predicados da DSL agregando condições com operadores booleanos (AND, OR, NOT).
/// </summary>
public sealed class RuleConditionGroup : IRuleCondition
{
    private readonly List<IRuleCondition> _conditions = new();

    /// <summary>
    /// Inicializa uma nova instância de <see cref="RuleConditionGroup"/>.
    /// </summary>
    public RuleConditionGroup(LogicalOperator logicalOperator, IEnumerable<IRuleCondition>? conditions = null)
    {
        LogicalOperator = logicalOperator;
        if (conditions != null)
        {
            _conditions.AddRange(conditions);
        }
    }

    /// <summary>
    /// Construtor sem parâmetros para serialização.
    /// </summary>
    public RuleConditionGroup()
    {
        LogicalOperator = LogicalOperator.And;
    }

    /// <summary>
    /// Operador lógico aplicado sobre as condições filhas.
    /// </summary>
    public LogicalOperator LogicalOperator { get; init; }

    /// <summary>
    /// Coleção de condições ou subgrupos integrantes.
    /// </summary>
    public IReadOnlyCollection<IRuleCondition> Conditions => _conditions.AsReadOnly();

    /// <summary>
    /// Adiciona uma condição filha ao grupo.
    /// </summary>
    public void AddCondition(IRuleCondition condition)
    {
        ArgumentNullException.ThrowIfNull(condition);
        _conditions.Add(condition);
    }

    /// <inheritdoc />
    public string Description =>
        $"({string.Join($" {LogicalOperator} ", _conditions.Select(c => c.Description))})";
}
