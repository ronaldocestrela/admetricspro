using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using Microsoft.Extensions.Options;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Insights;
using SocialPublishing.Application.Options;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Connections;

namespace SocialPublishing.Infrastructure.Insights;

/// <summary>
/// Adaptador HTTP que lê insights orgânicos na Graph API da Meta e na TikTok Content Posting API.
/// </summary>
public sealed class HttpOrganicInsightsReader : IOrganicInsightsReader
{
    private const string TikTokVideoQueryEndpoint = "https://open.tiktokapis.com/v2/video/query/";

    private readonly HttpClient _httpClient;
    private readonly OrganicOAuthOptions _options;

    /// <summary>
    /// Inicializa o leitor com cliente HTTP dedicado (sem loggers de URL).
    /// </summary>
    /// <param name="httpClient">Cliente HTTP nomeado do módulo.</param>
    /// <param name="options">Opções OAuth orgânicas (versão Graph).</param>
    public HttpOrganicInsightsReader(HttpClient httpClient, IOptions<OrganicOAuthOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public Task<Result<OrganicPostInsightSnapshot>> ReadAsync(
        OrganicInsightsReadRequest request,
        CancellationToken cancellationToken = default) =>
        request.Platform switch
        {
            OrganicPlatform.Instagram => ReadMetaAsync(
                request,
                "impressions,likes,comments,shares,saved",
                cancellationToken),
            OrganicPlatform.FacebookPage => ReadMetaAsync(
                request,
                "post_impressions,post_reactions_like_total,post_comments,post_shares",
                cancellationToken),
            OrganicPlatform.TikTok => ReadTikTokAsync(request, cancellationToken),
            _ => Task.FromResult(Result<OrganicPostInsightSnapshot>.Failure(SocialErrors.PlatformUnsupported)),
        };

    private async Task<Result<OrganicPostInsightSnapshot>> ReadMetaAsync(
        OrganicInsightsReadRequest request,
        string metrics,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PublishedExternalId) ||
            string.IsNullOrWhiteSpace(request.AccessToken))
        {
            return Result<OrganicPostInsightSnapshot>.Failure(SocialErrors.InsightsReadFailed);
        }

        var version = _options.Meta.GraphApiVersion;
        var url =
            $"https://graph.facebook.com/{version}/{Uri.EscapeDataString(request.PublishedExternalId)}/insights" +
            $"?metric={Uri.EscapeDataString(metrics)}";

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.AccessToken);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return Result<OrganicPostInsightSnapshot>.Failure(SocialErrors.TokenRevoked);
        }

        var bodyResult = await ReadBodyAsync(response, cancellationToken);
        if (bodyResult.IsFailure)
        {
            return Result<OrganicPostInsightSnapshot>.Failure(bodyResult.Error);
        }

        try
        {
            using var json = JsonDocument.Parse(bodyResult.Value);
            if (!json.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            {
                return Result<OrganicPostInsightSnapshot>.Failure(SocialErrors.InsightsReadFailed);
            }

            var metricsMap = ParseMetaMetrics(data);
            return request.Platform switch
            {
                OrganicPlatform.Instagram => MapInstagramSnapshot(metricsMap),
                OrganicPlatform.FacebookPage => MapFacebookSnapshot(metricsMap),
                _ => Result<OrganicPostInsightSnapshot>.Failure(SocialErrors.PlatformUnsupported),
            };
        }
        catch (JsonException)
        {
            return Result<OrganicPostInsightSnapshot>.Failure(SocialErrors.InsightsReadFailed);
        }
    }

    private async Task<Result<OrganicPostInsightSnapshot>> ReadTikTokAsync(
        OrganicInsightsReadRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PublishedExternalId) ||
            string.IsNullOrWhiteSpace(request.AccessToken))
        {
            return Result<OrganicPostInsightSnapshot>.Failure(SocialErrors.InsightsReadFailed);
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, TikTokVideoQueryEndpoint)
        {
            Content = JsonContent.Create(new
            {
                filters = new { video_ids = new[] { request.PublishedExternalId } },
            }),
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.AccessToken);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return Result<OrganicPostInsightSnapshot>.Failure(SocialErrors.TokenRevoked);
        }

        var bodyResult = await ReadBodyAsync(response, cancellationToken);
        if (bodyResult.IsFailure)
        {
            return Result<OrganicPostInsightSnapshot>.Failure(bodyResult.Error);
        }

        try
        {
            using var json = JsonDocument.Parse(bodyResult.Value);
            if (!json.RootElement.TryGetProperty("data", out var data) ||
                !data.TryGetProperty("videos", out var videos) ||
                videos.ValueKind != JsonValueKind.Array ||
                videos.GetArrayLength() == 0)
            {
                return Result<OrganicPostInsightSnapshot>.Failure(SocialErrors.InsightsReadFailed);
            }

            var video = videos[0];
            if (!TryReadLong(video, "view_count", out var impressions))
            {
                return Result<OrganicPostInsightSnapshot>.Failure(SocialErrors.InsightsReadFailed);
            }

            TryReadLong(video, "like_count", out var likes);
            TryReadLong(video, "comment_count", out var comments);
            TryReadLong(video, "share_count", out var shares);

            return Result<OrganicPostInsightSnapshot>.Success(
                new OrganicPostInsightSnapshot(impressions, likes, comments, shares, Saves: 0));
        }
        catch (JsonException)
        {
            return Result<OrganicPostInsightSnapshot>.Failure(SocialErrors.InsightsReadFailed);
        }
    }

    private static Dictionary<string, long> ParseMetaMetrics(JsonElement dataArray)
    {
        var map = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in dataArray.EnumerateArray())
        {
            if (!item.TryGetProperty("name", out var nameProp))
            {
                continue;
            }

            var name = nameProp.GetString();
            if (string.IsNullOrWhiteSpace(name) ||
                !item.TryGetProperty("values", out var values) ||
                values.ValueKind != JsonValueKind.Array ||
                values.GetArrayLength() == 0)
            {
                continue;
            }

            var firstValue = values[0];
            if (firstValue.TryGetProperty("value", out var valueProp) &&
                valueProp.TryGetInt64(out var metricValue))
            {
                map[name] = metricValue;
            }
        }

        return map;
    }

    private static Result<OrganicPostInsightSnapshot> MapInstagramSnapshot(IReadOnlyDictionary<string, long> metrics)
    {
        if (!metrics.TryGetValue("impressions", out var impressions))
        {
            return Result<OrganicPostInsightSnapshot>.Failure(SocialErrors.InsightsReadFailed);
        }

        metrics.TryGetValue("likes", out var likes);
        metrics.TryGetValue("comments", out var comments);
        metrics.TryGetValue("shares", out var shares);
        metrics.TryGetValue("saved", out var saves);

        return Result<OrganicPostInsightSnapshot>.Success(
            new OrganicPostInsightSnapshot(impressions, likes, comments, shares, saves));
    }

    private static Result<OrganicPostInsightSnapshot> MapFacebookSnapshot(IReadOnlyDictionary<string, long> metrics)
    {
        if (!metrics.TryGetValue("post_impressions", out var impressions))
        {
            return Result<OrganicPostInsightSnapshot>.Failure(SocialErrors.InsightsReadFailed);
        }

        metrics.TryGetValue("post_reactions_like_total", out var likes);
        metrics.TryGetValue("post_comments", out var comments);
        metrics.TryGetValue("post_shares", out var shares);

        return Result<OrganicPostInsightSnapshot>.Success(
            new OrganicPostInsightSnapshot(impressions, likes, comments, shares, Saves: 0));
    }

    private static async Task<Result<string>> ReadBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return Result<string>.Failure(SocialErrors.InsightsReadFailed);
        }

        return Result<string>.Success(body);
    }

    private static bool TryReadLong(JsonElement element, string propertyName, out long value)
    {
        value = 0;
        if (!element.TryGetProperty(propertyName, out var prop))
        {
            return false;
        }

        if (prop.TryGetInt64(out value))
        {
            return true;
        }

        if (prop.ValueKind == JsonValueKind.String &&
            long.TryParse(prop.GetString(), out value))
        {
            return true;
        }

        return false;
    }
}
