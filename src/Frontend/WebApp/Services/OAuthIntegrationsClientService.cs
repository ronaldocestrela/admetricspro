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
    private readonly ITenantSessionStateProvider _session;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Inicializa uma nova instância de <see cref="OAuthIntegrationsClientService"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP injetado.</param>
    /// <param name="tenantStateProvider">Provedor de estado do inquilino.</param>
    /// <param name="session">Sessão autenticada do circuito.</param>
    public OAuthIntegrationsClientService(HttpClient httpClient, ITenantStateProvider tenantStateProvider, ITenantSessionStateProvider session)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _tenantStateProvider = tenantStateProvider ?? throw new ArgumentNullException(nameof(tenantStateProvider));
        _session = session;
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
        CancellationToken cancellationToken = default,
        string? providerError = null)
    {
        try
        {
            var encodedCode = Uri.EscapeDataString(code);
            var encodedState = Uri.EscapeDataString(state);
            var encodedRedirect = Uri.EscapeDataString(redirectUri);
            var uri = $"/api/v1/integrations/oauth/callback?code={encodedCode}&state={encodedState}&redirectUri={encodedRedirect}";
            if (!string.IsNullOrEmpty(providerError)) uri += "&error=" + Uri.EscapeDataString(providerError);

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
        if (!string.IsNullOrWhiteSpace(_session.CurrentSession?.AccessToken))
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _session.CurrentSession.AccessToken);
        var tenantId = _tenantStateProvider.CurrentTenant?.TenantId;
        if (tenantId.HasValue && tenantId.Value != Guid.Empty)
        {
            request.Headers.TryAddWithoutValidation("X-Tenant-Id", tenantId.Value.ToString());
        }
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<Integrations.Domain.Meta.MetaAdAccount>>> GetMetaAdAccountsAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
        SendMetaAsync<IReadOnlyList<Integrations.Domain.Meta.MetaAdAccount>>(HttpMethod.Get, $"/api/v1/integrations/meta/ad-accounts?workspaceId={workspaceId}", null, cancellationToken);

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<Tenants.Application.Integrations.DTOs.ConnectedAdAccountDto>>> GetLinkedAdAccountsAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
        SendMetaAsync<IReadOnlyList<Tenants.Application.Integrations.DTOs.ConnectedAdAccountDto>>(HttpMethod.Get, $"/api/v1/integrations/connected-accounts?workspaceId={workspaceId}", null, cancellationToken);

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<Guid>>> SelectMetaAdAccountsAsync(Guid workspaceId, IReadOnlyList<string> accountIds, CancellationToken cancellationToken = default) =>
        SendMetaAsync<IReadOnlyList<Guid>>(HttpMethod.Post, "/api/v1/integrations/meta/ad-accounts/select", new { workspaceId, accountIds }, cancellationToken);

    /// <inheritdoc />
    public async Task<Result> DisconnectMetaAdAccountAsync(Guid workspaceId, Guid accountId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/integrations/meta/ad-accounts/{accountId}?workspaceId={workspaceId}");
            AppendTenantHeader(request);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            return await response.Content.ReadFromJsonAsync<Result>(JsonOptions, cancellationToken) ?? Result.Failure(Error.Failure("Meta.InvalidResponse", "Resposta inválida da API."));
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        { return Result.Failure(Error.Failure("Meta.NetworkError", "Não foi possível comunicar com a API.")); }
    }

    private async Task<Result<T>> SendMetaAsync<T>(HttpMethod method, string uri, object? body, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(method, uri);
            AppendTenantHeader(request);
            if (body is not null) request.Content = JsonContent.Create(body);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            return await response.Content.ReadFromJsonAsync<Result<T>>(JsonOptions, cancellationToken) ?? Result<T>.Failure(Error.Failure("Meta.InvalidResponse", "Resposta inválida da API."));
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        { return Result<T>.Failure(Error.Failure("Meta.NetworkError", "Não foi possível comunicar com a API.")); }
    }
}
