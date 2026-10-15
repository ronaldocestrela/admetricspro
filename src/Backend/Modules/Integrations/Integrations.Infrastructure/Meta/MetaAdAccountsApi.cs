using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using Integrations.Application.Options;
using Integrations.Domain.Meta;
using Microsoft.Extensions.Options;

namespace Integrations.Infrastructure.Meta;

/// <summary>Descoberta oficial de contas de anúncios usando a autorização contextual.</summary>
/// <param name="client">Cliente HTTP configurado.</param>
/// <param name="options">Configurações validadas da operação.</param>
public sealed class MetaAdAccountsApi(HttpClient client, IOptions<MetaAdsOAuthOptions> options) : IMetaAdAccountsApi
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<MetaAdAccount>>> ListAsync(string accessToken, CancellationToken cancellationToken)
    {
        var result = await new MetaGraphClient(client, options.Value).ReadCollectionAsync(
            "me/adaccounts?fields=id,name,account_status,currency,timezone_name,user_tasks&limit=100", accessToken, cancellationToken);
        if (result.IsFailure) return Result<IReadOnlyList<MetaAdAccount>>.Failure(result.Error);
        var accounts = new List<MetaAdAccount>();
        foreach (var row in result.Value)
        {
            var id = Read(row, "id");
            var name = Read(row, "name");
            var currency = Read(row, "currency");
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(currency))
                return Result<IReadOnlyList<MetaAdAccount>>.Failure(Error.Failure("Meta.InvalidResponse", "A Meta retornou uma conta sem identificação, nome ou moeda."));
            accounts.Add(new(id, name, currency, Read(row, "timezone_name"),
                row.TryGetProperty("account_status", out var status) && status.ValueKind == JsonValueKind.Number && status.TryGetInt32(out var value) ? value : null,
                row.TryGetProperty("user_tasks", out var tasks) && tasks.ValueKind == JsonValueKind.Array
                    ? tasks.EnumerateArray().Select(t => t.GetString()!).ToArray() : []));
        }
        return Result<IReadOnlyList<MetaAdAccount>>.Success(accounts.DistinctBy(a => a.Id).ToArray());
    }

    private static string? Read(JsonElement row, string field) => row.TryGetProperty(field, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
