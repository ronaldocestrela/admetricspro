using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using Integrations.Application.Options;

namespace Integrations.Infrastructure.Meta;

/// <summary>Transporte Graph com erros sanitizados, prova do aplicativo e paginação por cursor.</summary>
/// <param name="client">Cliente HTTP configurado.</param>
/// <param name="options">Configurações validadas da operação.</param>
public sealed class MetaGraphClient(HttpClient client, MetaAdsOAuthOptions options)
{
    /// <summary>Lê uma coleção inteira sem seguir URLs de paginação externas.</summary>
    /// <param name="pathAndQuery">Caminho relativo confiável e parâmetros de leitura.</param>
    /// <param name="token">Token do workspace.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Itens de todas as páginas ou falha sem dados sensíveis.</returns>
    public async Task<Result<IReadOnlyList<JsonElement>>> ReadCollectionAsync(string pathAndQuery, string token, CancellationToken cancellationToken)
    {
        var items = new List<JsonElement>();
        var cursors = new HashSet<string>(StringComparer.Ordinal);
        string? after = null;
        do
        {
            var path = pathAndQuery + (after is null ? "" : $"&after={Uri.EscapeDataString(after)}");
            var result = await GetAsync(path, token, cancellationToken);
            if (result.IsFailure) return Result<IReadOnlyList<JsonElement>>.Failure(result.Error);
            var root = result.Value;
            if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
                return Result<IReadOnlyList<JsonElement>>.Failure(Error.Failure("Meta.InvalidResponse", "A Meta retornou uma coleção inválida."));
            items.AddRange(data.EnumerateArray().Select(x => x.Clone()));
            after = null;
            if (root.TryGetProperty("paging", out var paging) && paging.TryGetProperty("next", out var next) &&
                next.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(next.GetString()))
            {
                if (!paging.TryGetProperty("cursors", out var cursor) || !cursor.TryGetProperty("after", out var afterValue) ||
                    string.IsNullOrWhiteSpace(afterValue.GetString()) || !cursors.Add(afterValue.GetString()!))
                    return Result<IReadOnlyList<JsonElement>>.Failure(Error.Failure("Meta.InvalidPagination", "A Meta retornou paginação inconsistente."));
                after = afterValue.GetString();
            }
        } while (after is not null);
        return Result<IReadOnlyList<JsonElement>>.Success(items);
    }

    /// <summary>Consulta um objeto Graph autenticado, repetindo somente leituras transitórias.</summary>
    /// <param name="pathAndQuery">Caminho relativo de uma operação oficial.</param>
    /// <param name="token">Token do workspace.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Objeto JSON ou erro sem segredos.</returns>
    public async Task<Result<JsonElement>> GetAsync(string pathAndQuery, string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Result<JsonElement>.Failure(Error.Unauthorized("Meta.TokenInvalid", "Reconecte a autorização Meta."));
        if (pathAndQuery.Contains("://", StringComparison.Ordinal) || pathAndQuery.StartsWith('/'))
            return Result<JsonElement>.Failure(Error.Validation("Meta.InvalidPath", "Caminho Meta inválido."));
        var proof = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(options.AppSecret), Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
        var url = $"https://graph.facebook.com/{options.GraphApiVersion}/{pathAndQuery}" +
                  (pathAndQuery.Contains('?') ? "&" : "?") + $"appsecret_proof={proof}";
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                using var response = await client.SendAsync(request, cancellationToken);
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                if (response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500)
                {
                    if (attempt >= 2) return Result<JsonElement>.Failure(Error.Failure("Meta.TransientFailure", "A Meta está temporariamente indisponível ou limitou as requisições."));
                    var retryDelay = response.Headers.RetryAfter?.Delta ??
                        (response.Headers.RetryAfter?.Date is { } until ? until - DateTimeOffset.UtcNow : TimeSpan.FromMilliseconds(250 * Math.Pow(2, attempt) + Random.Shared.Next(100)));
                    if (retryDelay > TimeSpan.FromSeconds(10)) return Result<JsonElement>.Failure(Error.Failure("Meta.RateLimited", "A Meta solicitou uma pausa maior. Tente novamente após o intervalo de limitação."));
                    await Task.Delay(retryDelay > TimeSpan.Zero ? retryDelay : TimeSpan.Zero, cancellationToken);
                    continue;
                }
                using var json = JsonDocument.Parse(content);
                var root = json.RootElement;
                if (root.ValueKind != JsonValueKind.Object) return Result<JsonElement>.Failure(Error.Failure("Meta.InvalidResponse", "A Meta retornou uma resposta inválida."));
                var hasError = root.TryGetProperty("error", out var error);
                if (response.IsSuccessStatusCode && !hasError) return Result<JsonElement>.Success(root.Clone());
                var code = hasError && error.TryGetProperty("code", out var codeValue) ? codeValue.GetInt32() : 0;
                var transient = response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500 ||
                    code is 1 or 2 or 4 or 17 or 32 or 613 ||
                    (hasError && error.TryGetProperty("is_transient", out var t) && t.ValueKind == JsonValueKind.True);
                if (transient && attempt < 2)
                {
                    var delay = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromMilliseconds(250 * Math.Pow(2, attempt) + Random.Shared.Next(100));
                    if (delay > TimeSpan.FromSeconds(10)) return Result<JsonElement>.Failure(Error.Failure("Meta.RateLimited", "A Meta solicitou uma pausa maior. Tente novamente após o intervalo de limitação."));
                    await Task.Delay(delay > TimeSpan.Zero ? delay : TimeSpan.Zero, cancellationToken);
                    continue;
                }
                return Result<JsonElement>.Failure(code switch
                {
                    190 => Error.Unauthorized("Meta.TokenInvalid", "A autorização Meta expirou ou foi revogada. Reconecte a conta."),
                    10 or 200 => Error.Forbidden("Meta.PermissionDenied", "A Meta não autorizou esta operação para a conta."),
                    100 => Error.Validation("Meta.InvalidRequest", "A Meta rejeitou os parâmetros desta operação."),
                    _ when transient => Error.Failure("Meta.TransientFailure", "A Meta está temporariamente indisponível ou limitou as requisições."),
                    _ => Error.Failure("Meta.ApiFailure", $"A Meta não concluiu a operação (HTTP {(int)response.StatusCode}, código {code}).")
                });
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            { return Result<JsonElement>.Failure(Error.Failure("Meta.Timeout", "A Meta excedeu o tempo de resposta.")); }
            catch (HttpRequestException) when (attempt < 2)
            { await Task.Delay(TimeSpan.FromMilliseconds(250 * Math.Pow(2, attempt)), cancellationToken); }
            catch (HttpRequestException)
            { return Result<JsonElement>.Failure(Error.Failure("Meta.Unavailable", "Não foi possível comunicar com a Meta.")); }
            catch (JsonException)
            { return Result<JsonElement>.Failure(Error.Failure("Meta.InvalidResponse", "A Meta retornou uma resposta inválida.")); }
        }
    }
}
