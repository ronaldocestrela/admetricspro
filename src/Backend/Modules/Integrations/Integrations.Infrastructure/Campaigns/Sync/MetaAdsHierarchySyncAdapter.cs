using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using Integrations.Application.Options;
using Integrations.Domain.Campaigns.Mappers;
using Integrations.Domain.Campaigns.Models;
using Integrations.Domain.Campaigns.Resilience;
using Integrations.Domain.Campaigns.Sync;
using Integrations.Infrastructure.Meta;
using Microsoft.Extensions.Options;

namespace Integrations.Infrastructure.Campaigns.Sync;

/// <summary>Sincroniza todas as páginas da hierarquia Meta usando a versão configurada.</summary>
public sealed class MetaAdsHierarchySyncAdapter : ICampaignHierarchySyncAdapter
{
    private readonly MetaGraphClient _graph;
    private readonly IMetaAdsHierarchyMapper _mapper;

    /// <summary>Reutiliza o transporte Meta para leitura paginada e erros sanitizados.</summary>
    /// <param name="httpClient">Cliente HTTP contextual.</param>
    /// <param name="mapper">Mapeamento da hierarquia unificada.</param>
    /// <param name="rateLimitPolicy">Contrato de resiliência existente; o transporte Meta trata retries por página.</param>
    /// <param name="options">Versão e segredo do aplicativo.</param>
    public MetaAdsHierarchySyncAdapter(HttpClient httpClient, IMetaAdsHierarchyMapper mapper,
        IHierarchyRateLimitPolicy rateLimitPolicy, IOptions<MetaAdsOAuthOptions> options)
    {
        _graph = new MetaGraphClient(httpClient, options.Value);
        _mapper = mapper;
    }

    /// <inheritdoc />
    public string Platform => "MetaAds";

    /// <inheritdoc />
    public async Task<Result<UnifiedCampaignHierarchy>> FetchHierarchyAsync(ConnectedAdAccount account,
        string? decryptedAccessToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(decryptedAccessToken))
            return Result<UnifiedCampaignHierarchy>.Failure(Error.Unauthorized("MetaAds.EmptyAccessToken", "Reconecte a autorização Meta."));
        var id = account.ExternalAccountId.StartsWith("act_", StringComparison.Ordinal) ? account.ExternalAccountId : $"act_{account.ExternalAccountId}";
        if (id.Length < 5 || !id[4..].All(char.IsAsciiDigit))
            return Result<UnifiedCampaignHierarchy>.Failure(Error.Validation("Meta.InvalidAccountId", "ID de conta Meta inválido."));
        var campaigns = await _graph.ReadCollectionAsync($"{id}/campaigns?fields=id,name,status,objective,daily_budget,lifetime_budget,start_time,stop_time&limit=100", decryptedAccessToken, cancellationToken);
        if (campaigns.IsFailure) return Result<UnifiedCampaignHierarchy>.Failure(campaigns.Error);
        var sets = await _graph.ReadCollectionAsync($"{id}/adsets?fields=id,name,campaign_id,status,bid_strategy,optimization_goal,daily_budget,lifetime_budget,targeting,start_time,end_time&limit=100", decryptedAccessToken, cancellationToken);
        if (sets.IsFailure) return Result<UnifiedCampaignHierarchy>.Failure(sets.Error);
        var ads = await _graph.ReadCollectionAsync($"{id}/ads?fields=id,name,adset_id,campaign_id,status,creative{{id,name,title,body,image_url,object_story_spec}}&limit=100", decryptedAccessToken, cancellationToken);
        if (ads.IsFailure) return Result<UnifiedCampaignHierarchy>.Failure(ads.Error);
        return _mapper.Map(JsonSerializer.Serialize(new { data = campaigns.Value }),
            JsonSerializer.Serialize(new { data = sets.Value }), JsonSerializer.Serialize(new { data = ads.Value }), account.Currency);
    }
}
