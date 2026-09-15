using System.Net.Http.Headers;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using Integrations.Domain.Campaigns.Mappers;
using Integrations.Domain.Campaigns.Models;
using Integrations.Domain.Campaigns.Resilience;
using Integrations.Domain.Campaigns.Sync;

namespace Integrations.Infrastructure.Campaigns.Sync;

/// <summary>
/// Adaptador concreto para sincronização de hierarquia estrutural com a TikTok Marketing API v1.3.
/// </summary>
public sealed class TikTokAdsHierarchySyncAdapter : ICampaignHierarchySyncAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ITikTokAdsHierarchyMapper _mapper;
    private readonly IHierarchyRateLimitPolicy _rateLimitPolicy;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TikTokAdsHierarchySyncAdapter"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP injetado.</param>
    /// <param name="mapper">Conversor de respostas do TikTok.</param>
    /// <param name="rateLimitPolicy">Política de rate limiting.</param>
    public TikTokAdsHierarchySyncAdapter(
        HttpClient httpClient,
        ITikTokAdsHierarchyMapper mapper,
        IHierarchyRateLimitPolicy rateLimitPolicy)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _rateLimitPolicy = rateLimitPolicy ?? throw new ArgumentNullException(nameof(rateLimitPolicy));
    }

    /// <inheritdoc />
    public string Platform => "TikTokAds";

    /// <inheritdoc />
    public async Task<Result<UnifiedCampaignHierarchy>> FetchHierarchyAsync(
        ConnectedAdAccount account,
        string? decryptedAccessToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(decryptedAccessToken))
        {
            return Result<UnifiedCampaignHierarchy>.Failure(
                Error.Unauthorized("TikTokAds.EmptyAccessToken", "Token de acesso do TikTok Ads não informado."));
        }

        var advertiserId = account.ExternalAccountId;

        return await _rateLimitPolicy.ExecuteAsync(async ct =>
        {
            try
            {
                using var cmpReq = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"https://business-api.tiktok.com/open_api/v1.3/campaign/get/?advertiser_id={advertiserId}&page_size=100");
                cmpReq.Headers.Add("Access-Token", decryptedAccessToken);
                using var cmpResp = await _httpClient.SendAsync(cmpReq, ct);
                var cmpJson = cmpResp.IsSuccessStatusCode ? await cmpResp.Content.ReadAsStringAsync(ct) : null;

                using var grpReq = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"https://business-api.tiktok.com/open_api/v1.3/adgroup/get/?advertiser_id={advertiserId}&page_size=100");
                grpReq.Headers.Add("Access-Token", decryptedAccessToken);
                using var grpResp = await _httpClient.SendAsync(grpReq, ct);
                var grpJson = grpResp.IsSuccessStatusCode ? await grpResp.Content.ReadAsStringAsync(ct) : null;

                using var adReq = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"https://business-api.tiktok.com/open_api/v1.3/ad/get/?advertiser_id={advertiserId}&page_size=100");
                adReq.Headers.Add("Access-Token", decryptedAccessToken);
                using var adResp = await _httpClient.SendAsync(adReq, ct);
                var adJson = adResp.IsSuccessStatusCode ? await adResp.Content.ReadAsStringAsync(ct) : null;

                return _mapper.Map(cmpJson, grpJson, adJson, account.Currency);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return Result<UnifiedCampaignHierarchy>.Failure(
                    Error.Failure("TikTokAds.HttpException", $"Erro de comunicação com TikTok Ads: {ex.Message}"));
            }
        }, Platform, cancellationToken);
    }
}
