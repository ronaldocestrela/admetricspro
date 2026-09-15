using System.Net.Http.Headers;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using Integrations.Domain.Campaigns.Mappers;
using Integrations.Domain.Campaigns.Models;
using Integrations.Domain.Campaigns.Resilience;
using Integrations.Domain.Campaigns.Sync;

namespace Integrations.Infrastructure.Campaigns.Sync;

/// <summary>
/// Adaptador concreto para busca paginada e sincronização da hierarquia estrutural com a Meta Ads Graph API v21.0.
/// </summary>
public sealed class MetaAdsHierarchySyncAdapter : ICampaignHierarchySyncAdapter
{
    private readonly HttpClient _httpClient;
    private readonly IMetaAdsHierarchyMapper _mapper;
    private readonly IHierarchyRateLimitPolicy _rateLimitPolicy;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="MetaAdsHierarchySyncAdapter"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP injetado.</param>
    /// <param name="mapper">Conversor de estruturas da Meta para o modelo unificado.</param>
    /// <param name="rateLimitPolicy">Política de controle de taxa e retentativas com backoff.</param>
    public MetaAdsHierarchySyncAdapter(
        HttpClient httpClient,
        IMetaAdsHierarchyMapper mapper,
        IHierarchyRateLimitPolicy rateLimitPolicy)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _rateLimitPolicy = rateLimitPolicy ?? throw new ArgumentNullException(nameof(rateLimitPolicy));
    }

    /// <inheritdoc />
    public string Platform => "MetaAds";

    /// <inheritdoc />
    public async Task<Result<UnifiedCampaignHierarchy>> FetchHierarchyAsync(
        ConnectedAdAccount account,
        string? decryptedAccessToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(decryptedAccessToken))
        {
            return Result<UnifiedCampaignHierarchy>.Failure(
                Error.Unauthorized("MetaAds.EmptyAccessToken", "Token de acesso da Meta Ads não informado ou expirado."));
        }

        var externalId = account.ExternalAccountId.StartsWith("act_", StringComparison.OrdinalIgnoreCase)
            ? account.ExternalAccountId
            : $"act_{account.ExternalAccountId}";

        return await _rateLimitPolicy.ExecuteAsync(async ct =>
        {
            try
            {
                using var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"https://graph.facebook.com/v21.0/{externalId}/campaigns?fields=id,name,status,objective,daily_budget,lifetime_budget,start_time,stop_time&limit=100");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", decryptedAccessToken);

                using var response = await _httpClient.SendAsync(request, ct);
                if (!response.IsSuccessStatusCode)
                {
                    var statusCode = (int)response.StatusCode;
                    var errBody = await response.Content.ReadAsStringAsync(ct);
                    return Result<UnifiedCampaignHierarchy>.Failure(
                        Error.Failure($"MetaAds.ApiError.{statusCode}", $"Falha ao consultar campanhas da Meta ({statusCode}): {errBody}"));
                }

                var campaignsJson = await response.Content.ReadAsStringAsync(ct);

                // Fetch adsets
                using var adSetsReq = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"https://graph.facebook.com/v21.0/{externalId}/adsets?fields=id,name,campaign_id,status,bid_strategy,optimization_goal,daily_budget,lifetime_budget,targeting&limit=100");
                adSetsReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", decryptedAccessToken);
                using var adSetsResp = await _httpClient.SendAsync(adSetsReq, ct);
                var adSetsJson = adSetsResp.IsSuccessStatusCode ? await adSetsResp.Content.ReadAsStringAsync(ct) : null;

                // Fetch ads
                using var adsReq = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"https://graph.facebook.com/v21.0/{externalId}/ads?fields=id,name,adset_id,campaign_id,status,creative{{id,name,title,body,image_url,object_story_spec}}&limit=100");
                adsReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", decryptedAccessToken);
                using var adsResp = await _httpClient.SendAsync(adsReq, ct);
                var adsJson = adsResp.IsSuccessStatusCode ? await adsResp.Content.ReadAsStringAsync(ct) : null;

                return _mapper.Map(campaignsJson, adSetsJson, adsJson, account.Currency);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return Result<UnifiedCampaignHierarchy>.Failure(
                    Error.Failure("MetaAds.HttpException", $"Erro de comunicação com Graph API: {ex.Message}"));
            }
        }, Platform, cancellationToken);
    }
}
