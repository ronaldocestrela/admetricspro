using System.Net.Http.Json;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using Integrations.Application.OAuth.DTOs;
using WebApp.State;

namespace WebApp.Services;

/// <summary>
/// Implementação concreta de <see cref="IOAuthIntegrationsClientService"/> consumindo a Web API.
/// </summary>
public sealed class OAuthIntegrationsClientService : IOAuthIntegrationsClientService
{
    private readonly HttpClient _httpClient;
    private readonly ITenantStateProvider _tenantStateProvider;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Inicializa uma nova instância de <see cref="OAuthIntegrationsClientService"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP injetado.</param>
    /// <param name="tenantStateProvider">Provedor de estado do inquilino.</param>
    public OAuthIntegrationsClientService(HttpClient httpClient, ITenantStateProvider tenantStateProvider)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _tenantStateProvider = tenantStateProvider ?? throw new ArgumentNullException(nameof(tenantStateProvider));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<OAuthConnectionStatusDto>>> GetConnectionsStatusAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var message = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/v1/integrations/oauth/status/{workspaceId}");
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<IReadOnlyList<OAuthConnectionStatusDto>>>(JsonOptions, cancellationToken);

            return result ?? Result<IReadOnlyList<OAuthConnectionStatusDto>>.Failure(
                Error.Failure("OAuth.InvalidResponse", "Resposta inválida ao obter status de conexões."));
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<OAuthConnectionStatusDto>>.Failure(
                Error.Failure("OAuth.NetworkError", $"Erro de comunicação ao obter status de conexões: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<int>> RefreshExpiringTokensAsync(
        int? thresholdHours = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var uri = thresholdHours.HasValue
                ? $"/api/v1/integrations/oauth/refresh?thresholdHours={thresholdHours.Value}"
                : "/api/v1/integrations/oauth/refresh";

            using var message = new HttpRequestMessage(HttpMethod.Post, uri);
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<int>>(JsonOptions, cancellationToken);

            return result ?? Result<int>.Failure(
                Error.Failure("OAuth.InvalidResponse", "Resposta inválida ao renovar credenciais."));
        }
        catch (Exception ex)
        {
            return Result<int>.Failure(
                Error.Failure("OAuth.NetworkError", $"Erro de comunicação ao renovar credenciais: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<OAuthAuthorizationUrlDto>> InitiateOAuthFlowAsync(
        Guid workspaceId,
        string platform,
        string redirectUri,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var encodedRedirect = Uri.EscapeDataString(redirectUri);
            var uri = $"/api/v1/integrations/oauth/authorize-url?workspaceId={workspaceId}&platform={platform}&redirectUri={encodedRedirect}";

            using var message = new HttpRequestMessage(HttpMethod.Get, uri);
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<OAuthAuthorizationUrlDto>>(JsonOptions, cancellationToken);

            return result ?? Result<OAuthAuthorizationUrlDto>.Failure(
                Error.Failure("OAuth.InvalidResponse", "Resposta inválida ao iniciar autorização."));
        }
        catch (Exception ex)
        {
            return Result<OAuthAuthorizationUrlDto>.Failure(
                Error.Failure("OAuth.NetworkError", $"Erro de comunicação ao gerar URL de autorização: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result> RevokeConnectionAsync(
        Guid workspaceId,
        string platform,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var message = new HttpRequestMessage(
                HttpMethod.Delete,
                $"/api/v1/integrations/oauth/{workspaceId}/{platform}");
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result>(JsonOptions, cancellationToken);

            return result ?? Result.Failure(
                Error.Failure("OAuth.InvalidResponse", "Resposta inválida ao revogar conexão."));
        }
        catch (Exception ex)
        {
            return Result.Failure(
                Error.Failure("OAuth.NetworkError", $"Erro de comunicação ao revogar conexão: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<OAuthConnectionStatusDto>> HandleOAuthCallbackAsync(
        string code,
        string state,
        string redirectUri,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var encodedCode = Uri.EscapeDataString(code);
            var encodedState = Uri.EscapeDataString(state);
            var encodedRedirect = Uri.EscapeDataString(redirectUri);
            var uri = $"/api/v1/integrations/oauth/callback?code={encodedCode}&state={encodedState}&redirectUri={encodedRedirect}";

            using var message = new HttpRequestMessage(HttpMethod.Get, uri);
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<OAuthConnectionStatusDto>>(JsonOptions, cancellationToken);

            return result ?? Result<OAuthConnectionStatusDto>.Failure(
                Error.Failure("OAuth.InvalidResponse", "Resposta inválida ao processar callback de autorização."));
        }
        catch (Exception ex)
        {
            return Result<OAuthConnectionStatusDto>.Failure(
                Error.Failure("OAuth.NetworkError", $"Erro de comunicação ao processar callback: {ex.Message}"));
        }
    }

    private void AppendTenantHeader(HttpRequestMessage request)
    {
        var tenantId = _tenantStateProvider.CurrentTenant?.TenantId;
        if (tenantId.HasValue && tenantId.Value != Guid.Empty)
        {
            request.Headers.TryAddWithoutValidation("X-Tenant-Id", tenantId.Value.ToString());
        }
    }
}
