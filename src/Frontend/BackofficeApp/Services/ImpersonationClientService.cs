using System.Net.Http.Json;
using BuildingBlocks.Domain.Primitives;

namespace BackofficeApp.Services;

/// <summary>
/// Implementação concreta de <see cref="IImpersonationClientService"/> consumindo a API REST do SaaS.
/// </summary>
public sealed class ImpersonationClientService : IImpersonationClientService
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ImpersonationClientService"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP configurado.</param>
    public ImpersonationClientService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task<Result> TerminateSessionAsync(
        Guid tenantId,
        Guid sessionId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new { Reason = reason };
            var response = await _httpClient.PostAsJsonAsync(
                $"/api/v1/tenants/{tenantId}/impersonate/{sessionId}/terminate",
                payload,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return Result.Success();
            }

            var errorResult = await response.Content.ReadFromJsonAsync<ResultEnvelope>(cancellationToken);
            return Result.Failure(errorResult?.Error ?? Error.Failure("Impersonation.TerminationFailed", "Falha ao encerrar a sessão de impersonation."));
        }
        catch (Exception ex)
        {
            return Result.Failure(Error.Failure("Impersonation.NetworkError", ex.Message));
        }
    }

    private sealed class ResultEnvelope
    {
        public bool IsSuccess { get; set; }
        public Error? Error { get; set; }
    }
}
