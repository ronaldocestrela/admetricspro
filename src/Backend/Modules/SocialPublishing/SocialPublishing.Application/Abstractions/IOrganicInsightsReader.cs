using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Application.Insights;

namespace SocialPublishing.Application.Abstractions;

/// <summary>
/// Porta de leitura de insights orgânicos nas APIs das redes (Meta Graph e TikTok).
/// </summary>
public interface IOrganicInsightsReader
{
    /// <summary>
    /// Obtém métricas atuais do post publicado na rede.
    /// </summary>
    /// <param name="request">Contexto de leitura com token já decifrado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Snapshot de métricas ou falha estável (<see cref="SocialPublishing.Domain.SocialErrors.InsightsReadFailed"/>).</returns>
    Task<Result<OrganicPostInsightSnapshot>> ReadAsync(
        OrganicInsightsReadRequest request,
        CancellationToken cancellationToken = default);
}
