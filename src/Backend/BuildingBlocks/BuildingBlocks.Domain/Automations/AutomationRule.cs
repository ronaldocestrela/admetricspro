using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Domain.Automations;

/// <summary>
/// Ação de mutação a ser disparada quando as condições da regra forem satisfeitas.
/// </summary>
public sealed class RuleAction
{
    /// <summary>
    /// Inicializa uma nova instância de <see cref="RuleAction"/>.
    /// </summary>
    public RuleAction(
        RuleActionType type,
        string? platform = null,
        Guid? targetEntityId = null,
        decimal? value = null,
        Guid? destinationEntityId = null)
    {
        Type = type;
        Platform = platform?.Trim();
        TargetEntityId = targetEntityId;
        Value = value;
        DestinationEntityId = destinationEntityId;
    }

    /// <summary>
    /// Construtor sem parâmetros para serialização.
    /// </summary>
    public RuleAction()
    {
        Type = RuleActionType.PauseCampaign;
    }

    /// <summary>
    /// Tipo de mutação a ser executada.
    /// </summary>
    public RuleActionType Type { get; init; }

    /// <summary>
    /// Plataforma alvo opcional.
    /// </summary>
    public string? Platform { get; init; }

    /// <summary>
    /// Identificador da entidade alvo primária.
    /// </summary>
    public Guid? TargetEntityId { get; init; }

    /// <summary>
    /// Valor monetário ou percentual do ajuste.
    /// </summary>
    public decimal? Value { get; init; }

    /// <summary>
    /// Identificador da entidade de destino em realocações orçamentárias.
    /// </summary>
    public Guid? DestinationEntityId { get; init; }
}

/// <summary>
/// Agregado de domínio que representa uma regra de automação cross-platform persistida no banco do inquilino.
/// </summary>
public sealed class AutomationRule : Entity<Guid>
{
    private readonly List<RuleAction> _actions = new();

    private AutomationRule(
        Guid id,
        Guid workspaceId,
        string name,
        string description,
        bool isEnabled,
        RuleConditionGroup conditionTree,
        IEnumerable<RuleAction> actions,
        DateTime createdAtUtc)
        : base(id)
    {
        WorkspaceId = workspaceId;
        Name = name;
        Description = description;
        IsEnabled = isEnabled;
        ConditionTree = conditionTree;
        _actions.AddRange(actions);
        CreatedAtUtc = createdAtUtc;
    }

    private AutomationRule()
        : base(Guid.Empty)
    {
        Name = string.Empty;
        Description = string.Empty;
        ConditionTree = new RuleConditionGroup();
    }

    /// <summary>
    /// Identificador do workspace.
    /// </summary>
    public Guid WorkspaceId { get; private set; }

    /// <summary>
    /// Nome amigável da regra.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Descrição da finalidade da automação.
    /// </summary>
    public string Description { get; private set; }

    /// <summary>
    /// Indica se a regra está ativa para execução automática.
    /// </summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// Raiz da árvore de predicados lógicos.
    /// </summary>
    public RuleConditionGroup ConditionTree { get; private set; }

    /// <summary>
    /// Ações de mutação configuradas.
    /// </summary>
    public IReadOnlyCollection<RuleAction> Actions => _actions.AsReadOnly();

    /// <summary>
    /// Data UTC do último disparo da regra.
    /// </summary>
    public DateTime? LastTriggeredAtUtc { get; private set; }

    /// <summary>
    /// Total acumulado de execuções com sucesso.
    /// </summary>
    public int ExecutionCount { get; private set; }

    /// <summary>
    /// Data UTC de criação.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Data UTC da última atualização cadastral.
    /// </summary>
    public DateTime? UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Cria uma nova instância de <see cref="AutomationRule"/> validando invariantes.
    /// </summary>
    public static Result<AutomationRule> Create(
        Guid id,
        Guid workspaceId,
        string name,
        string description,
        RuleConditionGroup conditionTree,
        IEnumerable<RuleAction> actions,
        bool isEnabled = true)
    {
        if (id == Guid.Empty)
        {
            return Result<AutomationRule>.Failure(
                Error.Validation("AutomationRule.EmptyId", "O identificador da regra não pode ser vazio."));
        }

        if (workspaceId == Guid.Empty)
        {
            return Result<AutomationRule>.Failure(
                Error.Validation("AutomationRule.EmptyWorkspaceId", "O workspace associado é obrigatório."));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<AutomationRule>.Failure(
                Error.Validation("AutomationRule.EmptyName", "O nome da regra de automação é obrigatório."));
        }

        if (conditionTree is null || conditionTree.Conditions.Count == 0)
        {
            return Result<AutomationRule>.Failure(
                Error.Validation("AutomationRule.EmptyConditions", "A regra deve conter pelo menos uma condição ou predicado."));
        }

        var actionList = actions?.ToList() ?? new List<RuleAction>();
        if (actionList.Count == 0)
        {
            return Result<AutomationRule>.Failure(
                Error.Validation("AutomationRule.EmptyActions", "A regra deve conter pelo menos uma ação configurada."));
        }

        var rule = new AutomationRule(
            id,
            workspaceId,
            name.Trim(),
            description?.Trim() ?? string.Empty,
            isEnabled,
            conditionTree,
            actionList,
            DateTime.UtcNow);

        return Result<AutomationRule>.Success(rule);
    }

    /// <summary>
    /// Atualiza os parâmetros da regra de automação.
    /// </summary>
    public Result UpdateDetails(
        string name,
        string description,
        RuleConditionGroup conditionTree,
        IEnumerable<RuleAction> actions)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(
                Error.Validation("AutomationRule.EmptyName", "O nome da regra não pode ser vazio."));
        }

        if (conditionTree is null || conditionTree.Conditions.Count == 0)
        {
            return Result.Failure(
                Error.Validation("AutomationRule.EmptyConditions", "A regra deve conter pelo menos uma condição."));
        }

        var actionList = actions?.ToList() ?? new List<RuleAction>();
        if (actionList.Count == 0)
        {
            return Result.Failure(
                Error.Validation("AutomationRule.EmptyActions", "A regra deve conter pelo menos uma ação configurada."));
        }

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        ConditionTree = conditionTree;
        _actions.Clear();
        _actions.AddRange(actionList);
        UpdatedAtUtc = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Ativa a regra.
    /// </summary>
    public void Enable()
    {
        IsEnabled = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Desativa a regra.
    /// </summary>
    public void Disable()
    {
        IsEnabled = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Registra a ocorrência de disparo da regra.
    /// </summary>
    public void RecordTriggered(DateTime triggeredAtUtc)
    {
        LastTriggeredAtUtc = triggeredAtUtc;
        ExecutionCount++;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
