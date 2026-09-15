using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using Integrations.Application.Options;
using Integrations.Domain.OAuth;
using Microsoft.Extensions.Options;

namespace Integrations.Infrastructure.OAuth.Adapters;

/// <summary>
/// Adaptador de autenticação OAuth2 para a Microsoft Advertising Platform (Bing Ads).
/// Utiliza os endpoints do Microsoft Identity Platform v2.0 com escopos msads.manage.
/// </summary>
public sealed class BingAdsOAuthAdapter : IOAuthAdapter
{
    private const string AuthorizeEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize";
    private const string TokenEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/token";
    private readonly HttpClient _httpClient;
    private readonly BingAdsOAuthOptions _options;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="BingAdsOAuthAdapter"/>.
    /// </summary>
    /// <param name="httpClient">Instância de HttpClient para requisições externas.</param>
    /// <param name="options">Opções de configuração do Azure / Microsoft Advertising.</param>
    public BingAdsOAuthAdapter(HttpClient httpClient, IOptions<BingAdsOAuthOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public string Platform => OAuthPlatform.BingAds;

    /// <inheritdoc />
    public string GetAuthorizationUrl(string state, string redirectUri)
    {
        var scope = Uri.EscapeDataString(_options.DefaultScopes);
        var encodedRedirectUri = Uri.EscapeDataString(redirectUri);
        var encodedState = Uri.EscapeDataString(state);

        return $"{AuthorizeEndpoint}" +
               $"?client_id={_options.ClientId}" +
               $"&response_type=code" +
               $"&redirect_uri={encodedRedirectUri}" +
               $"&response_mode=query" +
               $"&scope={scope}" +
               $"&state={encodedState}";
    }

    /// <inheritdoc />
    public async Task<Result<OAuthTokenResult>> ExchangeCodeAsync(
        string code,
        string redirectUri,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new Dictionary<string, string>
            {
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["code"] = code,
                ["grant_type"] = "authorization_code",
                ["redirect_uri"] = redirectUri,
                ["scope"] = _options.DefaultScopes
            };

            using var requestContent = new FormUrlEncodedContent(parameters);
            var response = await _httpClient.PostAsync(TokenEndpoint, requestContent, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = ExtractMicrosoftErrorMessage(json);
                return Result<OAuthTokenResult>.Failure(
                    Error.Failure("BingAds.OAuthFailed", $"Falha ao trocar código Microsoft/Bing Ads: {errorMsg}"));
            }

            using var doc = JsonDocument.Parse(json);
            var accessToken = doc.RootElement.GetProperty("access_token").GetString() ?? string.Empty;
            var refreshToken = doc.RootElement.TryGetProperty("refresh_token", out var rProp) ? rProp.GetString() : null;
            var scopes = doc.RootElement.TryGetProperty("scope", out var sProp) ? sProp.GetString() ?? _options.DefaultScopes : _options.DefaultScopes;

            DateTime? expiresAt = null;
            if (doc.RootElement.TryGetProperty("expires_in", out var expProp))
            {
                expiresAt = DateTime.UtcNow.AddSeconds(expProp.GetInt64());
            }

            return Result<OAuthTokenResult>.Success(new OAuthTokenResult(
                AccessToken: accessToken,
                RefreshToken: refreshToken,
                AccessTokenExpiresAtUtc: expiresAt,
                RefreshTokenExpiresAtUtc: null,
                Scopes: scopes));
        }
        catch (Exception ex)
        {
            return Result<OAuthTokenResult>.Failure(
                Error.Failure("BingAds.Exception", $"Exceção ao conectar com Microsoft Advertising: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<OAuthTokenResult>> RefreshTokenAsync(
        string refreshTokenOrCurrentToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new Dictionary<string, string>
            {
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshTokenOrCurrentToken,
                ["scope"] = _options.DefaultScopes
            };

            using var requestContent = new FormUrlEncodedContent(parameters);
            var response = await _httpClient.PostAsync(TokenEndpoint, requestContent, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = ExtractMicrosoftErrorMessage(json);
                return Result<OAuthTokenResult>.Failure(
                    Error.Failure("BingAds.RefreshFailed", $"Falha ao renovar token Bing Ads: {errorMsg}"));
            }

            using var doc = JsonDocument.Parse(json);
            var accessToken = doc.RootElement.GetProperty("access_token").GetString() ?? string.Empty;
            var refreshToken = doc.RootElement.TryGetProperty("refresh_token", out var rProp) ? rProp.GetString() : refreshTokenOrCurrentToken;
            var scopes = doc.RootElement.TryGetProperty("scope", out var sProp) ? sProp.GetString() ?? _options.DefaultScopes : _options.DefaultScopes;

            DateTime? expiresAt = null;
            if (doc.RootElement.TryGetProperty("expires_in", out var expProp))
            {
                expiresAt = DateTime.UtcNow.AddSeconds(expProp.GetInt64());
            }

            return Result<OAuthTokenResult>.Success(new OAuthTokenResult(
                AccessToken: accessToken,
                RefreshToken: refreshToken,
                AccessTokenExpiresAtUtc: expiresAt,
                RefreshTokenExpiresAtUtc: null,
                Scopes: scopes));
        }
        catch (Exception ex)
        {
            return Result<OAuthTokenResult>.Failure(
                Error.Failure("BingAds.Exception", $"Erro ao renovar token com Microsoft Advertising: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<Result> RevokeTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        // Microsoft Advertising / Entra v2.0 não possui endpoint público padrão de revogação imediata via token
        return Task.FromResult(Result.Success());
    }

    private static string ExtractMicrosoftErrorMessage(string jsonResponse)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonResponse);
            if (doc.RootElement.TryGetProperty("error_description", out var descProp))
            {
                return descProp.GetString() ?? jsonResponse;
            }
        }
        catch
        {
            // Ignora erro de parse
        }

        return jsonResponse;
    }
}
