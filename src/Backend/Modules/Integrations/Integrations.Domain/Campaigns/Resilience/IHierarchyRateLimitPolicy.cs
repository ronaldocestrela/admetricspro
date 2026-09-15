using BuildingBlocks.Domain.Primitives;

namespace Integrations.Domain.Campaigns.Resilience;

/// <summary>
/// Contrato para a política de resiliência e controle de taxa de requisições (rate limiting)
/// com tratamento de retentativas e backoff exponencial com jitter.
/// </summary>
public interface IHierarchyRateLimitPolicy
{
    /// <summary>
    /// Executa uma operação assíncrona protegida por política de retentativas com backoff exponencial.
    /// </summary>
    /// <typeparam name="T">Tipo do valor retornado em caso de sucesso.</typeparam>
    /// <param name="action">Função a ser executada com proteção contra rate limiting.</param>
    /// <param name="platform">Nome da plataforma (MetaAds, GoogleAds, TikTokAds, BingAds).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação encapsulado em <see cref="Result{T}"/>.</returns>
    Task<Result<T>> ExecuteAsync<T>(
        Func<CancellationToken, Task<Result<T>>> action,
        string platform,
        CancellationToken cancellationToken = default);
}
