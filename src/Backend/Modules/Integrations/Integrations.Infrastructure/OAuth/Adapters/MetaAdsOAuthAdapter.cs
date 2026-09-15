using System.Net.Http.Json;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using Integrations.Application.Options;
using Integrations.Domain.OAuth;
using Microsoft.Extensions.Options;

namespace Integrations.Infrastructure.OAuth.Adapters;

/// <summary>
/// Adaptador de autenticação OAuth2 para a Meta Graph API (Facebook e Instagram Ads).
/// Implementa a conversão de short-lived tokens em tokens de longa duração (60 dias)
/// e suporta renovação preventiva via endpoint fb_exchange_token.
/// </summary>
public sealed class MetaAdsOAuthAdapter : IOAuthAdapter
{
    private readonly HttpClient _httpClient;
    private readonly MetaAdsOAuthOptions _options;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="MetaAdsOAuthAdapter"/>.
    /// </summary>
    /// <param name="httpClient">Instância de HttpClient para requisições externas.</param>
    /// <param name="options">Opções de configuração do aplicativo Meta.</param>
    public MetaAdsOAuthAdapter(HttpClient httpClient, IOptions<MetaAdsOAuthOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public string Platform => OAuthPlatform.MetaAds;

    /// <inheritdoc />
    public string GetAuthorizationUrl(string state, string redirectUri)
    {
        var scope = Uri.EscapeDataString(_options.DefaultScopes);
        var encodedRedirectUri = Uri.EscapeDataString(redirectUri);
        var encodedState = Uri.EscapeDataString(state);

        return $"https://www.facebook.com/{_options.GraphApiVersion}/dialog/oauth" +
               $"?client_id={_options.AppId}" +
               $"&redirect_uri={encodedRedirectUri}" +
               $"&state={encodedState}" +
               $"&scope={scope}" +
               $"&response_type=code";
    }

    /// <inheritdoc />
    public async Task<Result<OAuthTokenResult>> ExchangeCodeAsync(
        string code,
        string redirectUri,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Troca o code por short-lived token
            var tokenUrl = $"https://graph.facebook.com/{_options.GraphApiVersion}/oauth/access_token" +
                           $"?client_id={_options.AppId}" +
                           $"&client_secret={_options.AppSecret}" +
                           $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                           $"&code={Uri.EscapeDataString(code)}";

            var response = await _httpClient.GetAsync(tokenUrl, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = ExtractMetaErrorMessage(content);
                return Result<OAuthTokenResult>.Failure(
                    Error.Failure("MetaAds.OAuthFailed", $"Falha ao trocar código de autorização Meta: {errorMsg}"));
            }

            using var doc = JsonDocument.Parse(content);
            var shortLivedToken = doc.RootElement.GetProperty("access_token").GetString() ?? string.Empty;

            // 2. Troca short-lived token por long-lived token (60 dias)
            var longLivedResult = await ExchangeForLongLivedTokenAsync(shortLivedToken, cancellationToken);
            if (longLivedResult.IsFailure)
            {
                return longLivedResult;
            }

            var finalToken = longLivedResult.Value.AccessToken;
            var expiresAt = longLivedResult.Value.AccessTokenExpiresAtUtc;

            // 3. Obtém dados do usuário/conta comercial via /me
            var (accountId, accountName) = await FetchUserProfileAsync(finalToken, cancellationToken);

            return Result<OAuthTokenResult>.Success(new OAuthTokenResult(
                AccessToken: finalToken,
                RefreshToken: null,
                AccessTokenExpiresAtUtc: expiresAt,
                RefreshTokenExpiresAtUtc: null,
                Scopes: _options.DefaultScopes,
                ExternalAccountId: accountId,
                ExternalAccountName: accountName));
        }
        catch (Exception ex)
        {
            return Result<OAuthTokenResult>.Failure(
                Error.Failure("MetaAds.Exception", $"Exceção de rede na comunicação com a Meta Graph API: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<OAuthTokenResult>> RefreshTokenAsync(
        string refreshTokenOrCurrentToken,
        CancellationToken cancellationToken = default)
    {
        return await ExchangeForLongLivedTokenAsync(refreshTokenOrCurrentToken, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result> RevokeTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var deleteUrl = $"https://graph.facebook.com/{_options.GraphApiVersion}/me/permissions" +
                            $"?access_token={Uri.EscapeDataString(token)}";

            var response = await _httpClient.DeleteAsync(deleteUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return Result.Failure(
                    Error.Failure("MetaAds.RevocationFailed", "Falha ao revogar permissões junto à Meta Graph API."));
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(
                Error.Failure("MetaAds.RevocationException", $"Erro ao tentar revogar token Meta: {ex.Message}"));
        }
    }

    private async Task<Result<OAuthTokenResult>> ExchangeForLongLivedTokenAsync(
        string currentToken,
        CancellationToken cancellationToken)
    {
        try
        {
            var exchangeUrl = $"https://graph.facebook.com/{_options.GraphApiVersion}/oauth/access_token" +
                              $"?grant_type=fb_exchange_token" +
                              $"&client_id={_options.AppId}" +
                              $"&client_secret={_options.AppSecret}" +
                              $"&fb_exchange_token={Uri.EscapeDataString(currentToken)}";

            var response = await _httpClient.GetAsync(exchangeUrl, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = ExtractMetaErrorMessage(content);
                return Result<OAuthTokenResult>.Failure(
                    Error.Failure("MetaAds.OAuthFailed", $"Falha ao obter token de longa duração da Meta: {errorMsg}"));
            }

            using var doc = JsonDocument.Parse(content);
            var longLivedToken = doc.RootElement.GetProperty("access_token").GetString() ?? string.Empty;

            DateTime? expiresAt = null;
            if (doc.RootElement.TryGetProperty("expires_in", out var expiresInElement))
            {
                var seconds = expiresInElement.GetInt64();
                expiresAt = DateTime.UtcNow.AddSeconds(seconds);
            }
            else
            {
                // Padrão Meta: 60 dias se não informado expressamente
                expiresAt = DateTime.UtcNow.AddDays(60);
            }

            return Result<OAuthTokenResult>.Success(new OAuthTokenResult(
                AccessToken: longLivedToken,
                RefreshToken: null,
                AccessTokenExpiresAtUtc: expiresAt,
                RefreshTokenExpiresAtUtc: null,
                Scopes: _options.DefaultScopes));
        }
        catch (Exception ex)
        {
            return Result<OAuthTokenResult>.Failure(
                Error.Failure("MetaAds.Exception", $"Erro na troca por token de longa duração: {ex.Message}"));
        }
    }

    private async Task<(string? Id, string? Name)> FetchUserProfileAsync(
        string token,
        CancellationToken cancellationToken)
    {
        try
        {
            var meUrl = $"https://graph.facebook.com/{_options.GraphApiVersion}/me?fields=id,name&access_token={Uri.EscapeDataString(token)}";
            var response = await _httpClient.GetAsync(meUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return (null, null);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(content);
            var id = doc.RootElement.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
            var name = doc.RootElement.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;

            return (id, name);
        }
        catch
        {
            return (null, null);
        }
    }

    private static string ExtractMetaErrorMessage(string jsonResponse)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonResponse);
            if (doc.RootElement.TryGetProperty("error", out var errorProp) &&
                errorProp.TryGetProperty("message", out var msgProp))
            {
                return msgProp.GetString() ?? jsonResponse;
            }
        }
        catch
        {
            // Ignora erro de parse
        }

        return jsonResponse;
    }
}
