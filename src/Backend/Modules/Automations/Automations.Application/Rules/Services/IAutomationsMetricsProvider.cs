using Automations.Domain.Services;

namespace Automations.Application.Rules.Services;

/// <summary>
/// Provedor de dados de métricas consolidadas em memória para avaliação de regras de automação.
/// </summary>
public interface IAutomationsMetricsProvider
{
    /// <summary>
    /// Carrega as fotografias de desempenho do workspace para a montagem do contexto de avaliação.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Contexto de avaliação com snapshots de métricas.</returns>
    Task<RuleEvaluationContext> LoadMetricsContextAsync(Guid workspaceId, CancellationToken cancellationToken = default);
}
