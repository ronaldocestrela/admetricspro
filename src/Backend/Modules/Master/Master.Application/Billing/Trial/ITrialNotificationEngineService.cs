using BuildingBlocks.Domain.Primitives;

namespace Master.Application.Billing.Trial;

/// <summary>
/// Contrato do motor de avaliação e disparo contínuo de notificações para o ciclo de vida de tenants em período de testes (Trial).
/// </summary>
public interface ITrialNotificationEngineService
{
    /// <summary>
    /// Executa o ciclo de avaliação de todos os tenants em trial, identificando marcos de 7, 3, 1 dias restantes ou expiração, despachando as notificações e gravando a auditoria.
    /// </summary>
    /// <param name="referenceDateUtc">Data e hora de referência opcional (padrão DateTime.UtcNow).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado com o sumário de execuções ou erro de negócio.</returns>
    Task<Result<TrialNoticeExecutionSummary>> ProcessTrialNoticesCycleAsync(
        DateTime? referenceDateUtc = null,
        CancellationToken cancellationToken = default);
}
