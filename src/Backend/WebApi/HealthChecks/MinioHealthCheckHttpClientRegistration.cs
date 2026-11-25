namespace WebApi.HealthChecks;

/// <summary>
/// Constantes de registro do <see cref="System.Net.Http.HttpClient"/> usado pelo probe MinIO.
/// </summary>
public static class MinioHealthCheckHttpClientRegistration
{
    /// <summary>
    /// Nome lógico do cliente HTTP do health check (timeout curto, sem credenciais).
    /// </summary>
    public const string HttpClientName = "MinioHealthCheck";

    /// <summary>
    /// Tempo máximo de espera por resposta do endpoint live do MinIO.
    /// </summary>
    public static readonly TimeSpan HealthCheckTimeout = TimeSpan.FromSeconds(2);
}