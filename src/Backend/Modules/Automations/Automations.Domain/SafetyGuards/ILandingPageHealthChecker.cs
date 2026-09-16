using BuildingBlocks.Domain.Primitives;

namespace Automations.Domain.SafetyGuards;

/// <summary>
/// Contrato de serviço de domínio para monitoramento de integridade de páginas de destino (Detector 404/500).
/// Executa testes periódicos nas URLs finais de anúncios ativos e efetua pausa preventiva de links quebrados.
/// </summary>
public interface ILandingPageHealthChecker
{
    /// <summary>
    /// Executa a verificação de integridade de todas as URLs finais de anúncios ativos do workspace especificado.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace associado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado contendo métricas da auditoria de links e incidentes mitigados.</returns>
    Task<Result<LandingPageCheckResult>> CheckAndMitigateLandingPagesAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default);
}
