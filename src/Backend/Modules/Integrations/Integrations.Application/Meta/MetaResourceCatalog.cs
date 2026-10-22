using System.Globalization;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using Integrations.Domain.Meta;

namespace Integrations.Application.Meta;

/// <summary>Referência oficial versionada compartilhada pela validação backend e pela apresentação.</summary>
public static class MetaResourceCatalog
{
    private static readonly IReadOnlyList<MetaResourceDefinition> Definitions = Load();
    private static readonly HashSet<string> TransportFields = new(StringComparer.OrdinalIgnoreCase)
        { "access_token", "app_secret", "appsecret", "appsecret_proof", "account_id", "app_id", "client_secret" };

    private static IReadOnlyList<MetaResourceDefinition> Load()
    {
        using var stream = typeof(MetaResourceCatalog).Assembly.GetManifestResourceStream(
            "Integrations.Application.Meta.meta-resource-schema.json")!;
        return JsonSerializer.Deserialize<MetaResourceDefinition[]>(stream)!;
    }

    /// <summary>Obtém a descrição de um recurso permitido.</summary>
    /// <param name="resource">Nome oficial do recurso.</param>
    /// <returns>Catálogo ou falha de validação.</returns>
    public static Result<MetaResourceDefinition> Get(string resource) =>
        Definitions.FirstOrDefault(x => x.Resource == resource) is { } definition ? Result<MetaResourceDefinition>.Success(definition)
        : Result<MetaResourceDefinition>.Failure(Error.Validation("Meta.InvalidResource", "Recurso Meta não suportado."));

    /// <summary>Valida tipos e disponibilidade por operação sem aceitar campos de transporte.</summary>
    /// <param name="resource">Recurso oficial.</param>
    /// <param name="editing">Indica edição de objeto existente.</param>
    /// <param name="data">Somente campos selecionados pelo usuário.</param>
    /// <returns>Sucesso ou erro específico de configuração.</returns>
    public static Result ValidateFields(string resource, bool editing, JsonElement data)
    {
        var definition = Get(resource);
        if (definition.IsFailure) return Result.Failure(definition.Error);
        if (data.ValueKind != JsonValueKind.Object || HasTransportFields(data)) return Invalid("Campos de credencial ou identidade da conta não podem ser enviados.");
        foreach (var property in data.EnumerateObject())
        {
            var field = definition.Value.Fields.FirstOrDefault(f => f.Name == property.Name);
            if (field is null || field.Restriction is not null || (editing ? !field.Update : !field.Create))
                return Invalid($"O campo {property.Name} não está disponível nesta operação.");
            if (!Matches(property.Value, field.Type, field.EnumValues)) return Invalid($"Tipo ou opção inválida em {field.Label} ({field.Name}).");
            if (!ValidateChildren(property.Value, field.Children)) return Invalid($"Estrutura inválida em {field.Label} ({field.Name}).");
        }
        return Result.Success();
    }

    private static bool ValidateChildren(JsonElement value, IReadOnlyList<MetaResourceField> children)
    {
        if (value.ValueKind == JsonValueKind.Array) return value.EnumerateArray().All(x => ValidateChildren(x, children));
        if (value.ValueKind != JsonValueKind.Object) return true;
        foreach (var property in value.EnumerateObject())
        {
            var child = children.FirstOrDefault(c => c.Name == property.Name);
            if (child is not null && (!Matches(property.Value, child.Type, child.EnumValues) || !ValidateChildren(property.Value, child.Children))) return false;
        }
        return true;
    }

    private static bool Matches(JsonElement value, string type, IReadOnlyList<string> options)
    {
        if (type.StartsWith("list<", StringComparison.Ordinal))
            return value.ValueKind == JsonValueKind.Array && value.EnumerateArray().All(x => Matches(x, type[5..^1], options));
        if (options.Count > 0) return value.ValueKind == JsonValueKind.String && options.Contains(value.GetString(), StringComparer.Ordinal);
        if (type.EndsWith("_enum", StringComparison.Ordinal) || type == "string") return value.ValueKind == JsonValueKind.String;
        if (type == "bool") return value.ValueKind is JsonValueKind.True or JsonValueKind.False;
        if (type is "int" or "unsigned int")
        {
            var text = value.ValueKind is JsonValueKind.Number or JsonValueKind.String ? value.ToString() : "";
            return decimal.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var number) &&
                number == decimal.Truncate(number) && number >= (type == "unsigned int" ? 0 : long.MinValue) && number <= ulong.MaxValue;
        }
        if (type == "float") return value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out _);
        if (type == "datetime") return value.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        return value.ValueKind == JsonValueKind.Object;
    }

    private static bool HasTransportFields(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Object => value.EnumerateObject().Any(p => TransportFields.Contains(p.Name) || HasTransportFields(p.Value)),
        JsonValueKind.Array => value.EnumerateArray().Any(HasTransportFields),
        _ => false
    };
    private static Result Invalid(string message) => Result.Failure(Error.Validation("Meta.InvalidWrite", message));
}
