using System.Net.Http.Headers;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Application.Connections;
using SocialPublishing.Application.Options;
using SocialPublishing.Domain;

namespace SocialPublishing.Infrastructure.OAuth;

/// <summary>
/// Troca de código e resolução de contas publicáveis na Graph API da Meta (Instagram e Facebook Page).
/// </summary>
internal static class MetaOrganicOAuthExchange
{
    private sealed record MetaManagedPage(
        string Id,
        string Name,
        string AccessToken,
        string? InstagramBusinessId,
        string? InstagramUsername);
    /// <summary>
    /// Troca o código por token long-lived e resolve a conta Instagram Professional/Business vinculada a uma Page.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP sem logging de URLs sensíveis.</param>
    /// <param name="options">Credenciais Meta orgânicas.</param>
    /// <param name="code">Código OAuth.</param>
    /// <param name="redirectUri">URI de retorno.</param>
    /// <param name="scopes">Escopos solicitados na autorização.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Grant com token de Page e metadados do Instagram Business.</returns>
    public static async Task<Result<OrganicAuthorizationGrant>> ExchangeInstagramAsync(
        HttpClient httpClient,
        MetaOrganicOAuthOptions options,
        string code,
        string redirectUri,
        string scopes,
        CancellationToken cancellationToken)
    {
        var configuration = ValidateConfiguration(options);
        if (configuration.IsFailure)
        {
            return Result<OrganicAuthorizationGrant>.Failure(configuration.Error);
        }

        var tokenResult = await ExchangeLongLivedTokenAsync(httpClient, options, code, redirectUri, cancellationToken);
        if (tokenResult.IsFailure)
        {
            return Result<OrganicAuthorizationGrant>.Failure(tokenResult.Error);
        }

        var accountsResult = await GetAccountsAsync(httpClient, options, tokenResult.Value.Token, cancellationToken);
        if (accountsResult.IsFailure)
        {
            return Result<OrganicAuthorizationGrant>.Failure(accountsResult.Error);
        }

        var candidates = accountsResult.Value
            .Where(p => !string.IsNullOrWhiteSpace(p.InstagramBusinessId) &&
                        !string.IsNullOrWhiteSpace(p.AccessToken))
            .Select(p => (IgId: p.InstagramBusinessId!, Username: p.InstagramUsername ?? p.InstagramBusinessId!, PageToken: p.AccessToken))
            .ToList();

        return candidates.Count switch
        {
            0 => Result<OrganicAuthorizationGrant>.Failure(SocialErrors.OrganicAccountNotFound),
            1 => Result<OrganicAuthorizationGrant>.Success(new OrganicAuthorizationGrant(
                candidates[0].PageToken,
                null,
                tokenResult.Value.Expiry,
                candidates[0].IgId,
                candidates[0].Username,
                scopes)),
            _ => Result<OrganicAuthorizationGrant>.Failure(SocialErrors.OrganicAccountAmbiguous),
        };
    }

    /// <summary>
    /// Troca o código por token long-lived e resolve a única Facebook Page publicável.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP sem logging de URLs sensíveis.</param>
    /// <param name="options">Credenciais Meta orgânicas.</param>
    /// <param name="code">Código OAuth.</param>
    /// <param name="redirectUri">URI de retorno.</param>
    /// <param name="scopes">Escopos solicitados na autorização.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Grant com token de Page.</returns>
    public static async Task<Result<OrganicAuthorizationGrant>> ExchangeFacebookPageAsync(
        HttpClient httpClient,
        MetaOrganicOAuthOptions options,
        string code,
        string redirectUri,
        string scopes,
        CancellationToken cancellationToken)
    {
        var configuration = ValidateConfiguration(options);
        if (configuration.IsFailure)
        {
            return Result<OrganicAuthorizationGrant>.Failure(configuration.Error);
        }

        var tokenResult = await ExchangeLongLivedTokenAsync(httpClient, options, code, redirectUri, cancellationToken);
        if (tokenResult.IsFailure)
        {
            return Result<OrganicAuthorizationGrant>.Failure(tokenResult.Error);
        }

        var accountsResult = await GetAccountsAsync(httpClient, options, tokenResult.Value.Token, cancellationToken);
        if (accountsResult.IsFailure)
        {
            return Result<OrganicAuthorizationGrant>.Failure(accountsResult.Error);
        }

        var pages = accountsResult.Value
            .Where(p => !string.IsNullOrWhiteSpace(p.Id) && !string.IsNullOrWhiteSpace(p.AccessToken))
            .Select(p => (p.Id, Name: p.Name, Token: p.AccessToken))
            .ToList();

        return pages.Count switch
        {
            0 => Result<OrganicAuthorizationGrant>.Failure(SocialErrors.OrganicAccountNotFound),
            1 => Result<OrganicAuthorizationGrant>.Success(new OrganicAuthorizationGrant(
                pages[0].Token,
                null,
                tokenResult.Value.Expiry,
                pages[0].Id,
                pages[0].Name,
                scopes)),
            _ => Result<OrganicAuthorizationGrant>.Failure(SocialErrors.OrganicAccountAmbiguous),
        };
    }

    private static Result ValidateConfiguration(MetaOrganicOAuthOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.AppId) || string.IsNullOrWhiteSpace(options.AppSecret))
        {
            return Result.Failure(SocialErrors.OAuthConfigurationMissing);
        }

        return Result.Success();
    }

    private static async Task<Result<(string Token, DateTime? Expiry)>> ExchangeLongLivedTokenAsync(
        HttpClient httpClient,
        MetaOrganicOAuthOptions options,
        string code,
        string redirectUri,
        CancellationToken cancellationToken)
    {
        try
        {
            var shortQuery =
                $"client_id={Uri.EscapeDataString(options.AppId)}" +
                $"&client_secret={Uri.EscapeDataString(options.AppSecret)}" +
                $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                $"&code={Uri.EscapeDataString(code)}";

            var shortResult = await ReadTokenAsync(httpClient, options.GraphApiVersion, shortQuery, cancellationToken);
            if (shortResult.IsFailure)
            {
                return shortResult;
            }

            var longQuery =
                $"grant_type=fb_exchange_token&client_id={Uri.EscapeDataString(options.AppId)}" +
                $"&client_secret={Uri.EscapeDataString(options.AppSecret)}" +
                $"&fb_exchange_token={Uri.EscapeDataString(shortResult.Value.Token)}";

            return await ReadTokenAsync(httpClient, options.GraphApiVersion, longQuery, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result<(string, DateTime?)>.Failure(SocialErrors.OAuthExchangeFailed);
        }
        catch (Exception)
        {
            return Result<(string, DateTime?)>.Failure(SocialErrors.OAuthExchangeFailed);
        }
    }

    private static async Task<Result<IReadOnlyList<MetaManagedPage>>> GetAccountsAsync(
        HttpClient httpClient,
        MetaOrganicOAuthOptions options,
        string userAccessToken,
        CancellationToken cancellationToken)
    {
        try
        {
            var fields = "id,name,access_token,instagram_business_account{id,username}";
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://graph.facebook.com/{options.GraphApiVersion}/me/accounts?fields={Uri.EscapeDataString(fields)}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userAccessToken);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return Result<IReadOnlyList<MetaManagedPage>>.Failure(SocialErrors.OAuthExchangeFailed);
            }

            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            if (!json.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            {
                return Result<IReadOnlyList<MetaManagedPage>>.Failure(SocialErrors.OAuthExchangeFailed);
            }

            var pages = new List<MetaManagedPage>();
            foreach (var page in data.EnumerateArray())
            {
                if (!page.TryGetProperty("id", out var idProp) ||
                    string.IsNullOrWhiteSpace(idProp.GetString()) ||
                    !page.TryGetProperty("access_token", out var tokenProp) ||
                    string.IsNullOrWhiteSpace(tokenProp.GetString()))
                {
                    continue;
                }

                var name = page.TryGetProperty("name", out var nameProp)
                    ? nameProp.GetString() ?? idProp.GetString()!
                    : idProp.GetString()!;

                string? igId = null;
                string? igUsername = null;
                if (page.TryGetProperty("instagram_business_account", out var igAccount) &&
                    igAccount.ValueKind == JsonValueKind.Object &&
                    igAccount.TryGetProperty("id", out var igIdProp))
                {
                    igId = igIdProp.GetString();
                    igUsername = igAccount.TryGetProperty("username", out var usernameProp)
                        ? usernameProp.GetString()
                        : null;
                }

                pages.Add(new MetaManagedPage(
                    idProp.GetString()!,
                    name,
                    tokenProp.GetString()!,
                    igId,
                    igUsername));
            }

            return Result<IReadOnlyList<MetaManagedPage>>.Success(pages);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result<IReadOnlyList<MetaManagedPage>>.Failure(SocialErrors.OAuthExchangeFailed);
        }
        catch (Exception)
        {
            return Result<IReadOnlyList<MetaManagedPage>>.Failure(SocialErrors.OAuthExchangeFailed);
        }
    }

    private static async Task<Result<(string Token, DateTime? Expiry)>> ReadTokenAsync(
        HttpClient httpClient,
        string graphApiVersion,
        string query,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            $"https://graph.facebook.com/{graphApiVersion}/oauth/access_token?{query}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return Result<(string, DateTime?)>.Failure(SocialErrors.OAuthExchangeFailed);
        }

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        if (!json.RootElement.TryGetProperty("access_token", out var tokenElement) ||
            string.IsNullOrWhiteSpace(tokenElement.GetString()))
        {
            return Result<(string, DateTime?)>.Failure(SocialErrors.OAuthExchangeFailed);
        }

        DateTime? expiry = null;
        if (json.RootElement.TryGetProperty("expires_in", out var expires) &&
            expires.TryGetInt64(out var seconds) &&
            seconds > 0)
        {
            expiry = DateTime.UtcNow.AddSeconds(seconds);
        }

        return Result<(string, DateTime?)>.Success((tokenElement.GetString()!, expiry));
    }
}
