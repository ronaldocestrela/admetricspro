using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using Integrations.Application.Options;
using Integrations.Domain.Meta;
using Microsoft.Extensions.Options;

namespace Integrations.Infrastructure.Meta;

/// <summary>Gerenciamento real de recursos Meta com verificação da conta e sem retries de escrita.</summary>
/// <param name="client">Cliente HTTP configurado.</param>
/// <param name="options">Configurações validadas da operação.</param>
public sealed class MetaManagementApi(HttpClient client, IOptions<MetaAdsOAuthOptions> options) : IMetaManagementApi
{
    private readonly MetaGraphClient _graph = new(client, options.Value);

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<JsonElement>>> SearchTargetingAsync(string type, string query, string token, CancellationToken cancellationToken)
    {
        if (type is not ("adinterest" or "adlocale" or "adgeolocation") || query.Length is < 2 or > 100)
            return Task.FromResult(Result<IReadOnlyList<JsonElement>>.Failure(Error.Validation("Meta.InvalidTargetingSearch", "Tipo de busca inválido ou termo fora do limite de 2 a 100 caracteres.")));
        return _graph.ReadCollectionAsync($"search?type={type}&q={Uri.EscapeDataString(query)}&limit=100" + (type == "adgeolocation" ? "&location_types=%5B%22country%22%5D" : ""), token, cancellationToken);
    }
    private static bool Numeric(string? id) => !string.IsNullOrWhiteSpace(id) && id.All(char.IsAsciiDigit);
    private static bool Account(string id) => id.StartsWith("act_", StringComparison.Ordinal) && Numeric(id[4..]);
    private static readonly IReadOnlyDictionary<string, string> Fields = new Dictionary<string, string>
    {
        ["campaigns"] = "id,account_id,name,status,effective_status,objective,special_ad_categories,daily_budget,lifetime_budget,bid_strategy,start_time,stop_time,updated_time",
        ["adsets"] = "id,account_id,campaign_id,name,status,effective_status,daily_budget,lifetime_budget,bid_strategy,optimization_goal,billing_event,targeting,promoted_object,start_time,end_time,updated_time",
        ["ads"] = "id,account_id,campaign_id,adset_id,name,status,effective_status,creative{id,name},issues_info,updated_time",
        ["adcreatives"] = "id,account_id,name,object_story_spec,asset_feed_spec,url_tags"
    };

    /// <inheritdoc />
    public async Task<Result<JsonElement>> GetAsync(string accountId, string resource, string externalId, string token, CancellationToken cancellationToken)
    {
        if (!Account(accountId) || !Numeric(externalId) || !Fields.TryGetValue(resource, out var fields))
            return Result<JsonElement>.Failure(Error.Validation("Meta.InvalidResource", "Recurso Meta inválido."));
        var result = await _graph.GetAsync($"{externalId}?fields={fields}", token, cancellationToken);
        if (result.IsFailure) return result;
        if (!result.Value.TryGetProperty("account_id", out var owner) || owner.GetString() != accountId[4..])
            return Result<JsonElement>.Failure(Error.Forbidden("Meta.ResourceAccountMismatch", "O recurso não pertence à conta vinculada a este workspace."));
        return result;
    }

    /// <inheritdoc />
    public async Task<Result<MetaWriteResult>> WriteAsync(string accountId, string resource, string? externalId, JsonElement data, string token, CancellationToken cancellationToken)
    {
        if (!Account(accountId) || !Fields.ContainsKey(resource) || (externalId is not null && !Numeric(externalId)) || data.ValueKind != JsonValueKind.Object)
            return Result<MetaWriteResult>.Failure(Error.Validation("Meta.InvalidResource", "Recurso ou dados Meta inválidos."));
        if (externalId is not null)
        {
            var owned = await GetAsync(accountId, resource, externalId, token, cancellationToken);
            if (owned.IsFailure) return Result<MetaWriteResult>.Failure(owned.Error);
        }
        if (externalId is null && resource is "adsets" or "ads")
        {
            var parentField = resource == "adsets" ? "campaign_id" : "adset_id";
            var parentResource = resource == "adsets" ? "campaigns" : "adsets";
            if (!data.TryGetProperty(parentField, out var parentId) || parentId.ValueKind != JsonValueKind.String)
                return Result<MetaWriteResult>.Failure(Error.Validation("Meta.ParentRequired", "Informe o recurso pai."));
            var parent = await GetAsync(accountId, parentResource, parentId.GetString()!, token, cancellationToken);
            if (parent.IsFailure) return Result<MetaWriteResult>.Failure(parent.Error);
            if (resource == "adsets" && parent.Value.TryGetProperty("special_ad_categories", out var categories) && categories.ValueKind == JsonValueKind.Array && categories.EnumerateArray().Any(c => c.GetString() != "NONE") &&
                data.TryGetProperty("targeting", out var targeting) && (targeting.TryGetProperty("age_min", out _) || targeting.TryGetProperty("age_max", out _) || targeting.TryGetProperty("genders", out _)))
                return Result<MetaWriteResult>.Failure(Error.Validation("Meta.SpecialCategoryTargeting", "Não restringir idade ou gênero em categorias especiais. As demais restrições serão validadas pela Meta."));
        }
        if (data.TryGetProperty("creative", out var creative) && creative.ValueKind == JsonValueKind.Object && creative.TryGetProperty("creative_id", out var creativeId))
        {
            var ownedCreative = await GetAsync(accountId, "adcreatives", creativeId.GetString()!, token, cancellationToken);
            if (ownedCreative.IsFailure) return Result<MetaWriteResult>.Failure(ownedCreative.Error);
        }
        if (resource == "adcreatives" && externalId is null)
        {
            foreach (var (asset, field, references) in new[]
            {
                ("promote_pages", "id", FindValues(data, "page_id").ToArray()),
                ("instagram_accounts", "id", FindValues(data, "instagram_user_id").ToArray()),
                ("adimages", "hash", FindValues(data, "image_hash").Concat(FindValues(data, "hash")).ToArray()),
                ("advideos", "id", FindValues(data, "video_id").ToArray())
            })
            {
                if (references.Length == 0) continue;
                var eligible = await ListAssetsAsync(accountId, asset, token, cancellationToken);
                if (eligible.IsFailure) return Result<MetaWriteResult>.Failure(eligible.Error);
                var ids = eligible.Value.Where(x => x.TryGetProperty(field, out _)).Select(x => x.GetProperty(field).GetString()).ToHashSet(StringComparer.Ordinal);
                if (references.Any(id => !ids.Contains(id)))
                    return Result<MetaWriteResult>.Failure(Error.Forbidden("Meta.AssetNotAuthorized", "Página ou mídia não pertence aos ativos autorizados para esta conta."));
                if (asset == "advideos" && eligible.Value.Any(v => references.Contains(v.GetProperty("id").GetString()) &&
                    v.TryGetProperty("status", out var videoStatus) && videoStatus.ValueKind == JsonValueKind.Object &&
                    videoStatus.TryGetProperty("video_status", out var status) && status.GetString() != "ready"))
                    return Result<MetaWriteResult>.Failure(Error.Validation("Meta.VideoNotReady", "Aguarde o processamento do vídeo antes de criar o criativo."));
            }
        }
        var parameters = data.EnumerateObject().Select(p => new KeyValuePair<string, string>(p.Name,
            p.Value.ValueKind == JsonValueKind.String ? p.Value.GetString()! : p.Value.GetRawText())).ToList();
        var proof = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(options.Value.AppSecret), Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
        parameters.Add(new("appsecret_proof", proof));
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post,
                $"https://graph.facebook.com/{options.Value.GraphApiVersion}/{externalId ?? accountId + "/" + resource}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new FormUrlEncodedContent(parameters);
            using var response = await client.SendAsync(request, cancellationToken);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            if (!response.IsSuccessStatusCode || json.RootElement.TryGetProperty("error", out _))
            {
                if ((int)response.StatusCode >= 500)
                    return Unknown();
                var message = "A Meta rejeitou a operação. Verifique permissões, campos, objetivo, segmentação e ativos da conta.";
                var code = 0;
                if (json.RootElement.TryGetProperty("error", out var failure))
                {
                    if (failure.TryGetProperty("code", out var errorCode) && errorCode.ValueKind == JsonValueKind.Number) code = errorCode.GetInt32();
                    if (failure.TryGetProperty("error_user_msg", out var userMessage) && userMessage.ValueKind == JsonValueKind.String && userMessage.GetString() is { } text)
                    {
                        message = text.Replace(token, "[credencial]", StringComparison.Ordinal);
                        if (!string.IsNullOrEmpty(options.Value.AppSecret)) message = message.Replace(options.Value.AppSecret, "[segredo]", StringComparison.Ordinal);
                        if (message.Length > 500) message = message[..500];
                    }
                }
                return Result<MetaWriteResult>.Failure(code switch
                {
                    190 => Error.Unauthorized("Meta.TokenInvalid", "A autorização Meta expirou ou foi revogada. Reconecte a conta."),
                    10 or 200 => Error.Forbidden("Meta.PermissionDenied", "A Meta não autorizou a operação nesta conta."),
                    _ => Error.Validation("Meta.WriteRejected", message)
                });
            }
            var id = json.RootElement.TryGetProperty("id", out var returnedId) ? returnedId.GetString() : externalId;
            var success = json.RootElement.TryGetProperty("success", out var confirmed) && confirmed.ValueKind == JsonValueKind.True;
            if (string.IsNullOrWhiteSpace(id) || (externalId is not null && !success)) return Unknown();
            return Result<MetaWriteResult>.Success(new(id, true));
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException or InvalidOperationException)
        {
            // O servidor pode ter processado o POST antes da falha ou cancelamento: nunca repetir.
            return Unknown();
        }
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<JsonElement>>> ListAssetsAsync(string accountId, string asset, string token, CancellationToken cancellationToken)
    {
        var fields = asset switch
        {
            "promote_pages" => "id,name",
            "instagram_accounts" => "id,username",
            "adspixels" => "id,name",
            "customaudiences" => "id,name,subtype",
            "adimages" => "hash,name,url",
            "advideos" => "id,title,status",
            _ => null
        };
        if (fields is null || !Account(accountId))
            return Task.FromResult(Result<IReadOnlyList<JsonElement>>.Failure(Error.Validation("Meta.UnsupportedAsset", "Tipo de ativo não suportado.")));
        return _graph.ReadCollectionAsync($"{accountId}/{asset}?fields={fields}&limit=100", token, cancellationToken);
    }

    private static Result<MetaWriteResult> Unknown() => Result<MetaWriteResult>.Failure(
        Error.Failure("Meta.WriteOutcomeUnknown", "O resultado da escrita é incerto. Consulte a conta na Meta antes de tentar uma nova operação."));

    private static IEnumerable<string> FindValues(JsonElement element, string field)
    {
        if (element.ValueKind == JsonValueKind.Object)
            foreach (var property in element.EnumerateObject())
            {
                if (property.Name == field && property.Value.ValueKind == JsonValueKind.String && property.Value.GetString() is { } value) yield return value;
                foreach (var nested in FindValues(property.Value, field)) yield return nested;
            }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var item in element.EnumerateArray()) foreach (var nested in FindValues(item, field)) yield return nested;
    }
}
