using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Application.Publishing;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Posts;

namespace SocialPublishing.Infrastructure.OAuth;

/// <summary>
/// Chamadas Content Posting API do TikTok para vídeo vertical.
/// </summary>
internal static class TikTokOrganicPublishHttp
{
    private const string InitEndpoint = "https://open.tiktokapis.com/v2/post/publish/video/init/";
    private const string StatusEndpoint = "https://open.tiktokapis.com/v2/post/publish/status/fetch/";

    /// <summary>
    /// Publica vídeo via PULL_FROM_URL e consulta status até conclusão imediata.
    /// </summary>
    public static async Task<Result<OrganicPublishReceipt>> PublishVideoAsync(
        HttpClient httpClient,
        OrganicPublishRequest request,
        string privacyLevel,
        CancellationToken cancellationToken)
    {
        if (request.Format != OrganicPostFormat.TikTokVideo)
        {
            return Result<OrganicPublishReceipt>.Failure(SocialErrors.PublishFormatUnsupported);
        }

        if (request.Media.Count == 0 || string.IsNullOrWhiteSpace(request.Media[0].SourceUrl))
        {
            return Result<OrganicPublishReceipt>.Failure(SocialErrors.PublishFailed);
        }

        try
        {
            using var initRequest = new HttpRequestMessage(HttpMethod.Post, InitEndpoint)
            {
                Content = JsonContent.Create(new
                {
                    post_info = new
                    {
                        title = request.Caption ?? string.Empty,
                        privacy_level = privacyLevel,
                    },
                    source_info = new
                    {
                        source = "PULL_FROM_URL",
                        video_url = request.Media[0].SourceUrl,
                    },
                }),
            };
            initRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.AccessToken);

            using var initResponse = await httpClient.SendAsync(initRequest, cancellationToken);
            var initBodyResult = await ReadTikTokBodyAsync(initResponse, cancellationToken);
            if (initBodyResult.IsFailure)
            {
                return Result<OrganicPublishReceipt>.Failure(initBodyResult.Error);
            }

            using var initJson = JsonDocument.Parse(initBodyResult.Value);
            if (!initJson.RootElement.TryGetProperty("data", out var data) ||
                !data.TryGetProperty("publish_id", out var publishIdProp) ||
                string.IsNullOrWhiteSpace(publishIdProp.GetString()))
            {
                return Result<OrganicPublishReceipt>.Failure(SocialErrors.PublishFailed);
            }

            var publishId = publishIdProp.GetString()!;

            using var statusRequest = new HttpRequestMessage(HttpMethod.Post, StatusEndpoint)
            {
                Content = JsonContent.Create(new { publish_id = publishId }),
            };
            statusRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.AccessToken);

            using var statusResponse = await httpClient.SendAsync(statusRequest, cancellationToken);
            var statusBodyResult = await ReadTikTokBodyAsync(statusResponse, cancellationToken);
            if (statusBodyResult.IsFailure)
            {
                return Result<OrganicPublishReceipt>.Failure(statusBodyResult.Error);
            }

            using var statusJson = JsonDocument.Parse(statusBodyResult.Value);
            if (!statusJson.RootElement.TryGetProperty("data", out var statusData))
            {
                return Result<OrganicPublishReceipt>.Failure(SocialErrors.PublishFailed);
            }

            var status = statusData.TryGetProperty("status", out var statusProp)
                ? statusProp.GetString()
                : null;

            if (string.Equals(status, "FAILED", StringComparison.OrdinalIgnoreCase))
            {
                return Result<OrganicPublishReceipt>.Failure(SocialErrors.PublishFailed);
            }

            if (string.Equals(status, "PUBLISH_COMPLETE", StringComparison.OrdinalIgnoreCase))
            {
                var externalId = statusData.TryGetProperty("publish_id", out var idProp) &&
                                 !string.IsNullOrWhiteSpace(idProp.GetString())
                    ? idProp.GetString()!
                    : publishId;
                return Result<OrganicPublishReceipt>.Success(new OrganicPublishReceipt(externalId));
            }

            return Result<OrganicPublishReceipt>.Failure(SocialErrors.PublishFailed);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result<OrganicPublishReceipt>.Failure(SocialErrors.PublishFailed);
        }
        catch (HttpRequestException)
        {
            return Result<OrganicPublishReceipt>.Failure(SocialErrors.PublishFailed);
        }
    }

    private static async Task<Result<string>> ReadTikTokBodyAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if ((int)response.StatusCode == 429)
        {
            return Result<string>.Failure(CreateRateLimitError(response));
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return Result<string>.Failure(SocialErrors.PublishFailed);
        }

        return Result<string>.Success(body);
    }

    private static PublishRateLimitedError CreateRateLimitError(HttpResponseMessage response)
    {
        TimeSpan? retryAfter = null;
        if (response.Headers.RetryAfter?.Delta is { } delta)
        {
            retryAfter = delta;
        }
        else if (response.Headers.TryGetValues("Retry-After", out var values) &&
                 int.TryParse(values.FirstOrDefault(), out var seconds) &&
                 seconds > 0)
        {
            retryAfter = TimeSpan.FromSeconds(seconds);
        }

        return new PublishRateLimitedError(SocialErrors.PublishRateLimited.Description, retryAfter);
    }
}
