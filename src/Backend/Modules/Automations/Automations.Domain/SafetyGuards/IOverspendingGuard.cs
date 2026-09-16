using BuildingBlocks.Domain.Primitives;

namespace Automations.Domain.SafetyGuards;

/// <summary>
/// Contrato de serviço de domínio para monitoramento e aplicação da trava de segurança de Overspending.
/// Avalia o consumo de verba em relação ao orçamento configurado e dispara pausas preventivas em caso de estouro (>120%).
/// </summary>
public interface IOverspendingGuard
{
    /// <summary>
    /// Limiar padrão de estouro orçamentário configurado no sistema (120% do orçamento diário).
    /// </summary>
    public const decimal DefaultOverspendingThreshold = 1.20m;

    /// <summary>
    /// Executa a auditoria de consumo orçamentário para todas as campanhas ativas do workspace especificado.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace associado.</param>
    /// <param name="thresholdMultiplier">Multiplicador de limite de estouro (padrão: 1.20 para 120%).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado contendo métricas consolidadas e incidentes mitigados.</returns>
    Task<Result<OverspendingCheckResult>> CheckAndMitigateOverspendingAsync(
        Guid workspaceId,
        decimal thresholdMultiplier = DefaultOverspendingThreshold,
        CancellationToken cancellationToken = default);
}
