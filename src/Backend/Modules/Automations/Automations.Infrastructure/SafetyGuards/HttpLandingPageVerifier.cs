using System.Net;
using Automations.Domain.SafetyGuards;

namespace Automations.Infrastructure.SafetyGuards;

/// <summary>
/// Implementação concreta do testador de integridade HTTP de Landing Pages.
/// Realiza requisições HTTP HEAD rápidas com fallback automático para GET (caso o servidor retorne 405).
/// </summary>
public sealed class HttpLandingPageVerifier : IHttpLandingPageVerifier
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="HttpLandingPageVerifier"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP configurado com timeout adequado.</param>
    public HttpLandingPageVerifier(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public async Task<LandingPageProbeResult> ProbeUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return new LandingPageProbeResult(IsHealthy: false, StatusCode: null, ErrorMessage: "URL vazia ou nula.");
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsedUri) ||
            (parsedUri.Scheme != Uri.UriSchemeHttp && parsedUri.Scheme != Uri.UriSchemeHttps))
        {
            return new LandingPageProbeResult(IsHealthy: false, StatusCode: null, ErrorMessage: "URL em formato inválido.");
        }

        try
        {
            // 1. Tentar HTTP HEAD inicial
            using var headRequest = new HttpRequestMessage(HttpMethod.Head, parsedUri);
            headRequest.Headers.Add("User-Agent", "AdMetricsPro-HealthChecker/1.0 (+https://admetricspro.com/bot)");

            using var headResponse = await _httpClient.SendAsync(headRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (headResponse.StatusCode == HttpStatusCode.MethodNotAllowed)
            {
                // 2. Fallback para HTTP GET com leitura apenas de cabeçalhos
                using var getRequest = new HttpRequestMessage(HttpMethod.Get, parsedUri);
                getRequest.Headers.Add("User-Agent", "AdMetricsPro-HealthChecker/1.0 (+https://admetricspro.com/bot)");

                using var getResponse = await _httpClient.SendAsync(getRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                return EvaluateResponse(getResponse);
            }

            return EvaluateResponse(headResponse);
        }
        catch (TaskCanceledException)
        {
            return new LandingPageProbeResult(IsHealthy: false, StatusCode: null, ErrorMessage: "Tempo limite de conexão esgotado (Timeout).");
        }
        catch (HttpRequestException ex)
        {
            var statusCode = ex.StatusCode.HasValue ? (int)ex.StatusCode.Value : (int?)null;
            return new LandingPageProbeResult(IsHealthy: false, StatusCode: statusCode, ErrorMessage: ex.Message);
        }
        catch (Exception ex)
        {
            return new LandingPageProbeResult(IsHealthy: false, StatusCode: null, ErrorMessage: ex.Message);
        }
    }

    private static LandingPageProbeResult EvaluateResponse(HttpResponseMessage response)
    {
        var statusCode = (int)response.StatusCode;

        // HTTP 2xx e 3xx são considerados saudáveis
        if (response.IsSuccessStatusCode || ((int)response.StatusCode >= 300 && (int)response.StatusCode < 400))
        {
            return new LandingPageProbeResult(IsHealthy: true, StatusCode: statusCode, ErrorMessage: response.ReasonPhrase ?? "OK");
        }

        // HTTP 4xx e 5xx são erros
        return new LandingPageProbeResult(
            IsHealthy: false,
            StatusCode: statusCode,
            ErrorMessage: response.ReasonPhrase ?? $"Erro HTTP {statusCode}");
    }
}
