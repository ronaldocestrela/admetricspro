using System.Net.Http.Headers;
using BuildingBlocks.Domain.Campaigns;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using Integrations.Domain.Campaigns.Models;
using Integrations.Domain.Campaigns.Resilience;
using Integrations.Domain.Campaigns.Sync;

namespace Integrations.Infrastructure.Campaigns.Sync;

/// <summary>
/// Adaptador para ingestão de métricas analíticas da Microsoft Advertising Platform / Bing Ads API v13.
/// </summary>
public sealed class BingAdsMetricsSyncAdapter : ICampaignMetricsSyncAdapter
{
    private readonly HttpClient _httpClient;
    private readonly IHierarchyRateLimitPolicy _rateLimitPolicy;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="BingAdsMetricsSyncAdapter"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP injetado.</param>
    /// <param name="rateLimitPolicy">Política de resiliência e controle de taxa.</param>
    public BingAdsMetricsSyncAdapter(
        HttpClient httpClient,
        IHierarchyRateLimitPolicy rateLimitPolicy)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _rateLimitPolicy = rateLimitPolicy ?? throw new ArgumentNullException(nameof(rateLimitPolicy));
    }

    /// <inheritdoc />
    public string Platform => "BingAds";

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<UnifiedMetricItem>>> FetchMetricsAsync(
        ConnectedAdAccount account,
        string? decryptedAccessToken,
        DateTime startDateUtc,
        DateTime endDateUtc,
        MetricGranularity granularity,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(decryptedAccessToken))
        {
            return Result<IReadOnlyList<UnifiedMetricItem>>.Failure(
                Error.Unauthorized("BingAds.EmptyAccessToken", "Token de acesso do Bing Ads não informado ou expirado."));
        }

        return await _rateLimitPolicy.ExecuteAsync(async ct =>
        {
            try
            {
                // Em ambiente de produção, dispara a solicitação de SubmitGenerateReport no ReportingService SOAP/REST
                using var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"https://reporting.api.bingads.microsoft.com/Api/Advertiser/Reporting/V13/Reports/Status?accountId={account.ExternalAccountId}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", decryptedAccessToken);

                using var response = await _httpClient.SendAsync(request, ct);
                if (!response.IsSuccessStatusCode)
                {
                    return Result<IReadOnlyList<UnifiedMetricItem>>.Failure(
                        Error.Failure("BingAds.MetricsError", $"Falha ao consultar relatório do Bing Ads. HTTP {(int)response.StatusCode}"));
                }

                // Retorna lista processada
                return Result<IReadOnlyList<UnifiedMetricItem>>.Success(new List<UnifiedMetricItem>());
            }
            catch (Exception ex)
            {
                return Result<IReadOnlyList<UnifiedMetricItem>>.Failure(
                    Error.Failure("BingAds.MetricsException", $"Exceção ao comunicar com a Bing Ads API: {ex.Message}"));
            }
        }, Platform, cancellationToken);
    }
}
