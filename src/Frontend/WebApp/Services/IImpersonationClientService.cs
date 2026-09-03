using BuildingBlocks.Domain.Primitives;

namespace WebApp.Services;

/// <summary>
/// Serviço de cliente HTTP para consumo de operações de impersonation contra a WebApi.
/// </summary>
public interface IImpersonationClientService
{
    /// <summary>
    /// Solicita a revogação de uma sessão de impersonation ativa no backend.
    /// </summary>
    /// <param name="tenantId">Identificador do tenant sob impersonação.</param>
    /// <param name="sessionId">Identificador da sessão ativa a ser revogada.</param>
    /// <param name="reason">Justificativa do encerramento.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado com sucesso ou falha.</returns>
    Task<Result> TerminateSessionAsync(
        Guid tenantId,
        Guid sessionId,
        string? reason = null,
        CancellationToken cancellationToken = default);
}
