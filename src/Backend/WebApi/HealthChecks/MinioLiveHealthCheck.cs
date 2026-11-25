using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace WebApi.HealthChecks;

/// <summary>
/// Verifica disponibilidade do processo MinIO via endpoint interno <c>/minio/health/live</c>.
/// </summary>
public sealed class MinioLiveHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _serviceUrl;

    /// <summary>
    /// Inicializa o probe com a URL base do serviço MinIO.
    /// </summary>
    /// <param name="httpClientFactory">Fábrica HTTP compartilhada do host.</param>
    /// <param name="serviceUrl">Endpoint interno (ex.: <c>http://minio:9000</c>).</param>
    public MinioLiveHealthCheck(IHttpClientFactory httpClientFactory, string serviceUrl)
    {
        _httpClientFactory = httpClientFactory;
        _serviceUrl = serviceUrl.TrimEnd('/');
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_serviceUrl))
        {
            return HealthCheckResult.Unhealthy("O endpoint do MinIO não está configurado.");
        }

        var liveUri = new Uri($"{_serviceUrl}/minio/health/live");

        try
        {
            var client = _httpClientFactory.CreateClient(MinioHealthCheckHttpClientRegistration.HttpClientName);
            using var response = await client.GetAsync(liveUri, cancellationToken).ConfigureAwait(false);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("O cluster MinIO respondeu ao health check.")
                : HealthCheckResult.Unhealthy("O cluster MinIO não está disponível.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return HealthCheckResult.Unhealthy("O cluster MinIO não está disponível.");
        }
    }
}