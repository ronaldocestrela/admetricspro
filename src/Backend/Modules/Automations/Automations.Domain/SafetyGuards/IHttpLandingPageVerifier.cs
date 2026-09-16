namespace Automations.Domain.SafetyGuards;

/// <summary>
/// Resultado da sondagem HTTP de integridade de uma Landing Page.
/// </summary>
/// <param name="IsHealthy">Indica se a URL respondeu com sucesso (HTTP 2xx ou 3xx).</param>
/// <param name="StatusCode">Código de status HTTP retornado ou nulo em falha de conexão.</param>
/// <param name="ErrorMessage">Mensagem descritiva do erro, falha ou status HTTP.</param>
public sealed record LandingPageProbeResult(
    bool IsHealthy,
    int? StatusCode,
    string ErrorMessage);

/// <summary>
/// Contrato do serviço de sondagem de URLs de Landing Pages via requisições HTTP HEAD/GET.
/// </summary>
public interface IHttpLandingPageVerifier
{
    /// <summary>
    /// Executa o teste de conectividade e status HTTP da URL informada.
    /// </summary>
    /// <param name="url">URL pública de destino do anúncio.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado com indicativo de saúde, status code e diagnóstico.</returns>
    Task<LandingPageProbeResult> ProbeUrlAsync(string url, CancellationToken cancellationToken = default);
}
