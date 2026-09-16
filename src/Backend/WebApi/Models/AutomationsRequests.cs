using BuildingBlocks.Domain.Automations;

namespace WebApi.Models;

/// <summary>
/// Payload para criação de nova regra de automação cross-platform.
/// </summary>
public sealed class CreateRuleApiRequest
{
    /// <summary>
    /// Identificador do workspace.
    /// </summary>
    public Guid WorkspaceId { get; init; }

    /// <summary>
    /// Nome amigável da regra.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Descrição da finalidade operacional da regra.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Árvore de condições e predicados da DSL.
    /// </summary>
    public RuleConditionGroup ConditionTree { get; init; } = new();

    /// <summary>
    /// Ações de mutação a serem disparadas quando os gatilhos forem satisfeitos.
    /// </summary>
    public List<RuleAction> Actions { get; init; } = new();

    /// <summary>
    /// Indica se a regra deve ser criada já ativa para avaliação.
    /// </summary>
    public bool IsEnabled { get; init; } = true;
}

/// <summary>
/// Payload para atualização dos dados de uma regra de automação existente.
/// </summary>
public sealed class UpdateRuleApiRequest
{
    /// <summary>
    /// Identificador do workspace associado.
    /// </summary>
    public Guid WorkspaceId { get; init; }

    /// <summary>
    /// Novo nome da regra.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Nova descrição da regra.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Nova árvore de condições da DSL.
    /// </summary>
    public RuleConditionGroup ConditionTree { get; init; } = new();

    /// <summary>
    /// Nova lista de ações de mutação.
    /// </summary>
    public List<RuleAction> Actions { get; init; } = new();
}

/// <summary>
/// Payload para alternância do estado de ativação de uma regra de automação.
/// </summary>
public sealed class ToggleRuleApiRequest
{
    /// <summary>
    /// Identificador do workspace associado.
    /// </summary>
    public Guid WorkspaceId { get; init; }

    /// <summary>
    /// Novo estado de ativação da regra (true para ativa, false para inativa).
    /// </summary>
    public bool IsEnabled { get; init; }
}

/// <summary>
/// Payload para solicitação de avaliação e execução imediata de uma regra.
/// </summary>
public sealed class EvaluateRuleApiRequest
{
    /// <summary>
    /// Identificador do workspace associado.
    /// </summary>
    public Guid WorkspaceId { get; init; }
}
