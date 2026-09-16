using BuildingBlocks.Domain.Automations;
using BuildingBlocks.Domain.Primitives;

namespace Automations.Application.Rules.Services;

/// <summary>
/// Contrato do despachador de ações de mutação geradas por regras de automação.
/// Traduz ações da DSL em comandos in-memory para o módulo Integrations via MediatR.
/// </summary>
public interface IRuleActionDispatcher
{
    /// <summary>
    /// Despacha as ações da regra para as entidades afetadas.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace associado.</param>
    /// <param name="actions">Coleção de ações configuradas.</param>
    /// <param name="matchedEntityIds">Identificadores das entidades que satisfizeram o gatilho.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Quantidade de comandos de mutação disparados com sucesso.</returns>
    Task<Result<int>> DispatchActionsAsync(
        Guid workspaceId,
        IEnumerable<RuleAction> actions,
        IReadOnlyList<Guid> matchedEntityIds,
        CancellationToken cancellationToken = default);
}
