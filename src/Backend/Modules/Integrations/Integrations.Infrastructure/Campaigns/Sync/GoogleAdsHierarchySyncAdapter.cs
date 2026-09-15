using System.Net.Http.Headers;
using System.Text;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using Integrations.Domain.Campaigns.Mappers;
using Integrations.Domain.Campaigns.Models;
using Integrations.Domain.Campaigns.Resilience;
using Integrations.Domain.Campaigns.Sync;

namespace Integrations.Infrastructure.Campaigns.Sync;

/// <summary>
/// Adaptador concreto para busca de hierarquia na Google Ads API via GAQL searchStream.
/// </summary>
public sealed class GoogleAdsHierarchySyncAdapter : ICampaignHierarchySyncAdapter
{
    private readonly HttpClient _httpClient;
    private readonly IGoogleAdsHierarchyMapper _mapper;
    private readonly IHierarchyRateLimitPolicy _rateLimitPolicy;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GoogleAdsHierarchySyncAdapter"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP injetado.</param>
    /// <param name="mapper">Conversor de respostas do Google Ads.</param>
    /// <param name="rateLimitPolicy">Política de rate limiting.</param>
    public GoogleAdsHierarchySyncAdapter(
        HttpClient httpClient,
        IGoogleAdsHierarchyMapper mapper,
        IHierarchyRateLimitPolicy rateLimitPolicy)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _rateLimitPolicy = rateLimitPolicy ?? throw new ArgumentNullException(nameof(rateLimitPolicy));
    }

    /// <inheritdoc />
    public string Platform => "GoogleAds";

    /// <inheritdoc />
    public async Task<Result<UnifiedCampaignHierarchy>> FetchHierarchyAsync(
        ConnectedAdAccount account,
        string? decryptedAccessToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(decryptedAccessToken))
        {
            return Result<UnifiedCampaignHierarchy>.Failure(
                Error.Unauthorized("GoogleAds.EmptyAccessToken", "Token de acesso do Google Ads não informado."));
        }

        var customerId = account.ExternalAccountId.Replace("-", string.Empty);

        return await _rateLimitPolicy.ExecuteAsync(async ct =>
        {
            try
            {
                var query = """
                SELECT 
                  campaign.id, 
                  campaign.name, 
                  campaign.status, 
                  campaign.advertising_channel_type, 
                  campaign_budget.amount_micros,
                  ad_group.id, 
                  ad_group.name, 
                  ad_group.status, 
                  ad_group.type,
                  ad_group_ad.ad.id, 
                  ad_group_ad.ad.name, 
                  ad_group_ad.status, 
                  ad_group_ad.ad.type, 
                  ad_group_ad.ad.final_urls, 
                  ad_group_ad.ad.responsive_search_ad.headlines, 
                  ad_group_ad.ad.responsive_search_ad.descriptions 
                FROM ad_group_ad 
                WHERE campaign.status != 'REMOVED'
                """;

                var jsonPayload = $"{{\"query\": \"{query.Replace("\r", "").Replace("\n", " ").Replace("\"", "\\\"")}\"}}";
                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"https://googleads.googleapis.com/v18/customers/{customerId}/googleAds:searchStream")
                {
                    Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
                };

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", decryptedAccessToken);
                using var response = await _httpClient.SendAsync(request, ct);

                if (!response.IsSuccessStatusCode)
                {
                    var statusCode = (int)response.StatusCode;
                    var errBody = await response.Content.ReadAsStringAsync(ct);
                    return Result<UnifiedCampaignHierarchy>.Failure(
                        Error.Failure($"GoogleAds.ApiError.{statusCode}", $"Erro Google Ads API ({statusCode}): {errBody}"));
                }

                var responseJson = await response.Content.ReadAsStringAsync(ct);
                return _mapper.Map(responseJson, account.Currency);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return Result<UnifiedCampaignHierarchy>.Failure(
                    Error.Failure("GoogleAds.HttpException", $"Erro de comunicação com Google Ads: {ex.Message}"));
            }
        }, Platform, cancellationToken);
    }
}
