using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using BuildingBlocks.Domain.Primitives;

namespace Integrations.Application.Meta;

/// <summary>Valida os campos suportados e exige confirmação explícita para ativação.</summary>
public static class MetaWriteValidation
{
    /// <summary>Valida e normaliza uma criação ou edição Meta antes de qualquer escrita externa.</summary>
    /// <param name="resource">Tipo de recurso.</param>
    /// <param name="externalId">Objeto editado, nulo para criação.</param>
    /// <param name="data">Campos oficiais da operação.</param>
    /// <param name="confirmActivation">Confirmação explícita de possível gasto.</param>
    /// <returns>Campos validados ou erro de negócio.</returns>
    public static Result<JsonElement> Validate(string resource, string? externalId, JsonElement data, bool confirmActivation)
    {
        var fields = MetaResourceCatalog.ValidateFields(resource, externalId is not null, data);
        if (fields.IsFailure) return Result<JsonElement>.Failure(fields.Error);
        if (data.GetRawText().Length > 100_000 ||
            data.EnumerateObject().GroupBy(p => p.Name, StringComparer.Ordinal).Any(g => g.Count() > 1)) return Invalid("Campos desconhecidos, duplicados ou recurso não suportado.");
        var obj = JsonNode.Parse(data.GetRawText())!.AsObject();
        if (obj.Count == 0) return Invalid("Informe os campos da alteração.");
        if (obj.TryGetPropertyValue("name", out var name) && (name is not JsonValue || !name.AsValue().TryGetValue<string>(out var text) || string.IsNullOrWhiteSpace(text) || text.Length > 200))
            return Invalid("Informe um nome válido de até 200 caracteres.");
        if (externalId is null && obj["name"] is null) return Invalid("O nome é obrigatório para criação.");
        foreach (var budget in new[] { "daily_budget", "lifetime_budget", "bid_amount", "spend_cap" })
            if (obj[budget] is { } amount && (!long.TryParse(amount.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out var number) || number < 0 || number == 0 && externalId is null && budget != "spend_cap"))
                return Invalid("Orçamentos e lances devem usar unidades monetárias mínimas inteiras; zero explícito depende da operação aceita pela Meta.");
        if (obj["daily_budget"] is { } daily && obj["lifetime_budget"] is { } lifetime &&
            decimal.Parse(daily.ToString(), CultureInfo.InvariantCulture) > 0 && decimal.Parse(lifetime.ToString(), CultureInfo.InvariantCulture) > 0)
            return Invalid("Escolha orçamento diário ou vitalício.");
        if (obj["status"] is { } status)
        {
            if (status.ToString() is not ("ACTIVE" or "PAUSED" or "ARCHIVED" or "DELETED")) return Invalid("Status Meta inválido.");
            if (status.ToString() == "ACTIVE" && (!confirmActivation || externalId is null)) return Invalid("Crie o recurso pausado. Ativação posterior exige confirmação explícita.");
        }
        if (externalId is null && resource != "adcreatives") obj["status"] = "PAUSED";
        if (resource == "campaigns")
        {
            if (obj["is_adset_budget_sharing_enabled"] is { } sharing &&
                (sharing is not JsonValue sharingValue || !sharingValue.TryGetValue<bool>(out _)))
                return Invalid("O compartilhamento de orçamento deve ser true ou false.");
            if (externalId is null && obj["daily_budget"] is null && obj["lifetime_budget"] is null && obj["is_adset_budget_sharing_enabled"] is null)
                obj["is_adset_budget_sharing_enabled"] = false;
            if (externalId is null && (obj["objective"] is null || obj["special_ad_categories"] is not JsonArray)) return Invalid("Objetivo e categorias especiais são obrigatórios.");
            if (obj["objective"] is { } objective && objective.ToString() is not ("OUTCOME_AWARENESS" or "OUTCOME_TRAFFIC" or "OUTCOME_ENGAGEMENT" or "OUTCOME_LEADS" or "OUTCOME_APP_PROMOTION" or "OUTCOME_SALES")) return Invalid("Objetivo não suportado pelo fluxo de criação Meta.");
            if (obj["special_ad_categories"] is { } categories && (categories is not JsonArray list || list.Any(c => c?.ToString() is not ("CREDIT" or "EMPLOYMENT" or "FINANCIAL_PRODUCTS_SERVICES" or "HOUSING" or "ISSUES_ELECTIONS_POLITICS" or "ONLINE_GAMBLING_AND_GAMING" or "NONE")))) return Invalid("Categorias especiais inválidas.");
        }
        if (resource == "adsets")
        {
            if (externalId is null && new[] { "campaign_id", "billing_event", "optimization_goal", "targeting" }.Any(p => obj[p] is null)) return Invalid("Campanha, cobrança, otimização e segmentação são obrigatórios.");
            if (obj["targeting"] is { } targeting && targeting is not JsonObject) return Invalid("Segmentação deve ser um objeto.");
            if (obj["targeting"] is JsonObject target)
            {
                if (target["geo_locations"] is not JsonObject) return Invalid("Informe localização geográfica oficial.");
                foreach (var age in new[] { "age_min", "age_max" })
                    if (target[age] is { } a && (!int.TryParse(a.ToString(), out var value) || value is < 13 or > 65)) return Invalid("Idade de segmentação inválida.");
                if (target["age_min"] is { } min && target["age_max"] is { } max && int.Parse(min.ToString()) > int.Parse(max.ToString())) return Invalid("Faixa etária inválida.");
            }
            if (obj["start_time"] is { } start && (!DateTimeOffset.TryParse(start.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var startTime) ||
                (obj["end_time"] is { } end && (!DateTimeOffset.TryParse(end.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var endTime) || endTime <= startTime)))) return Invalid("Datas do conjunto inválidas.");
        }
        if (resource == "ads" && externalId is null && (obj["adset_id"] is null || obj["creative"] is not JsonObject)) return Invalid("Informe conjunto e criativo para o anúncio.");
        if (resource == "ads" && obj["creative"] is JsonObject creative &&
            (creative["creative_id"] is not JsonValue creativeId || !creativeId.TryGetValue<string>(out var id) || string.IsNullOrWhiteSpace(id) || !id.All(char.IsAsciiDigit)))
            return Invalid("Selecione o ID de um criativo autorizado. Crie conteúdos no formulário de criativos.");
        if (resource == "adcreatives" && externalId is null && obj["object_story_spec"] is not JsonObject && obj["asset_feed_spec"] is not JsonObject &&
            obj["object_story_id"] is null && obj["source_facebook_post_id"] is null && obj["source_instagram_media_id"] is null &&
            obj["object_id"] is null && obj["image_hash"] is null && obj["image_url"] is null && obj["object_url"] is null && obj["link_url"] is null && obj["product_set_id"] is null)
            return Invalid("Informe a especificação do criativo ou uma publicação existente.");
        return Result<JsonElement>.Success(JsonSerializer.SerializeToElement(obj));
    }

    private static Result<JsonElement> Invalid(string message) => Result<JsonElement>.Failure(Error.Validation("Meta.InvalidWrite", message));
}
