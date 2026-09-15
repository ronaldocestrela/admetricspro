using System.Net.Http.Headers;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using Integrations.Domain.Campaigns.Mappers;
using Integrations.Domain.Campaigns.Models;
using Integrations.Domain.Campaigns.Resilience;
using Integrations.Domain.Campaigns.Sync;

namespace Integrations.Infrastructure.Campaigns.Sync;

/// <summary>
/// Adaptador concreto para busca de hierarquia na Microsoft Advertising / Bing Ads API.
/// </summary>
public sealed class BingAdsHierarchySyncAdapter : ICampaignHierarchySyncAdapter
{
    private readonly HttpClient _httpClient;
    private readonly IBingAdsHierarchyMapper _mapper;
    private readonly IHierarchyRateLimitPolicy _rateLimitPolicy;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="BingAdsHierarchySyncAdapter"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP injetado.</param>
    /// <param name="mapper">Conversor de respostas do Bing Ads.</param>
    /// <param name="rateLimitPolicy">Política de rate limiting.</param>
    public BingAdsHierarchySyncAdapter(
        HttpClient httpClient,
        IBingAdsHierarchyMapper mapper,
        IHierarchyRateLimitPolicy rateLimitPolicy)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _rateLimitPolicy = rateLimitPolicy ?? throw new ArgumentNullException(nameof(rateLimitPolicy));
    }

    /// <inheritdoc />
    public string Platform => "BingAds";

    /// <inheritdoc />
    public async Task<Result<UnifiedCampaignHierarchy>> FetchHierarchyAsync(
        ConnectedAdAccount account,
        string? decryptedAccessToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(decryptedAccessToken))
        {
            return Result<UnifiedCampaignHierarchy>.Failure(
                Error.Unauthorized("BingAds.EmptyAccessToken", "Token de acesso do Bing Ads não informado."));
        }

        var accountId = account.ExternalAccountId;

        return await _rateLimitPolicy.ExecuteAsync(async ct =>
        {
            try
            {
                using var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"https://campaign.api.bingads.microsoft.com/Api/Advertiser/CampaignManagement/v13/Campaigns?accountId={accountId}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", decryptedAccessToken);

                using var response = await _httpClient.SendAsync(request, ct);
                if (!response.IsSuccessStatusCode)
                {
                    var statusCode = (int)response.StatusCode;
                    var errBody = await response.Content.ReadAsStringAsync(ct);
                    return Result<UnifiedCampaignHierarchy>.Failure(
                        Error.Failure($"BingAds.ApiError.{statusCode}", $"Erro Bing Ads API ({statusCode}): {errBody}"));
                }

                var responseJson = await response.Content.ReadAsStringAsync(ct);
                return _mapper.Map(responseJson, null, null, account.Currency);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return Result<UnifiedCampaignHierarchy>.Failure(
                    Error.Failure("BingAds.HttpException", $"Erro de comunicação com Bing Ads: {ex.Message}"));
            }
        }, Platform, cancellationToken);
    }
}
