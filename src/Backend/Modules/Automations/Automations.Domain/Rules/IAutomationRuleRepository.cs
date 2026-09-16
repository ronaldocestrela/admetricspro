using BuildingBlocks.Domain.Automations;
using BuildingBlocks.Domain.Primitives;

namespace Automations.Domain.Rules;

/// <summary>
/// Contrato do repositório operacional de regras de automação persistidas no banco dedicado do inquilino.
/// </summary>
public interface IAutomationRuleRepository
{
    /// <summary>
    /// Adiciona uma nova regra de automação à base de dados.
    /// </summary>
    /// <param name="rule">Instância da regra.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação com a regra persistida.</returns>
    Task<Result<AutomationRule>> AddAsync(AutomationRule rule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém uma regra de automação pelo seu identificador primário.
    /// </summary>
    /// <param name="ruleId">Identificador único da regra.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Regra localizada ou nulo.</returns>
    Task<AutomationRule?> GetByIdAsync(Guid ruleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém todas as regras de automação cadastradas no workspace.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Coleção de regras encontradas.</returns>
    Task<IReadOnlyList<AutomationRule>> GetByWorkspaceIdAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove uma regra de automação da base de dados.
    /// </summary>
    /// <param name="ruleId">Identificador da regra a remover.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da exclusão.</returns>
    Task<Result> DeleteAsync(Guid ruleId, CancellationToken cancellationToken = default);
}
