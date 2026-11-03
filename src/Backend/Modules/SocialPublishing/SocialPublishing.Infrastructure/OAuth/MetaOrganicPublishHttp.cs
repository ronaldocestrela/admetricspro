using System.Net;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Application.Publishing;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Posts;

namespace SocialPublishing.Infrastructure.OAuth;

/// <summary>
/// Chamadas Graph API para publicação orgânica Meta (Page e Instagram).
/// </summary>
internal static class MetaOrganicPublishHttp
{
    /// <summary>
    /// Publica imagem única em Facebook Page.
    /// </summary>
    public static Task<Result<OrganicPublishReceipt>> PublishFacebookFeedImageAsync(
        HttpClient httpClient,
        string graphApiVersion,
        OrganicPublishRequest request,
        CancellationToken cancellationToken) =>
        PublishPagePhotoAsync(httpClient, graphApiVersion, request, cancellationToken);

    /// <summary>
    /// Publica carrossel ou Reel no Instagram Professional.
    /// </summary>
    public static Task<Result<OrganicPublishReceipt>> PublishInstagramAsync(
        HttpClient httpClient,
        string graphApiVersion,
        OrganicPublishRequest request,
        CancellationToken cancellationToken) =>
        request.Format switch
        {
            OrganicPostFormat.FeedCarousel => PublishInstagramCarouselAsync(
                httpClient,
                graphApiVersion,
                request,
                cancellationToken),
            OrganicPostFormat.Reel => PublishInstagramReelAsync(
                httpClient,
                graphApiVersion,
                request,
                cancellationToken),
            _ => Task.FromResult(Result<OrganicPublishReceipt>.Failure(SocialErrors.PublishFormatUnsupported)),
        };

    private static async Task<Result<OrganicPublishReceipt>> PublishPagePhotoAsync(
        HttpClient httpClient,
        string graphApiVersion,
        OrganicPublishRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Media.Count == 0 || string.IsNullOrWhiteSpace(request.Media[0].SourceUrl))
        {
            return Result<OrganicPublishReceipt>.Failure(SocialErrors.PublishFailed);
        }

        var url =
            $"https://graph.facebook.com/{graphApiVersion}/{Uri.EscapeDataString(request.ExternalAccountId)}/photos";
        var form = new Dictionary<string, string>
        {
            ["url"] = request.Media[0].SourceUrl,
            ["caption"] = request.Caption ?? string.Empty,
            ["published"] = "true",
            ["access_token"] = request.AccessToken,
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new FormUrlEncodedContent(form),
        };

        return await SendMetaPublishAsync(httpClient, httpRequest, cancellationToken);
    }

    private static async Task<Result<OrganicPublishReceipt>> PublishInstagramCarouselAsync(
        HttpClient httpClient,
        string graphApiVersion,
        OrganicPublishRequest request,
        CancellationToken cancellationToken)
    {
        var childIds = new List<string>();
        foreach (var item in request.Media.OrderBy(m => m.SortOrder))
        {
            var childResult = await CreateInstagramMediaContainerAsync(
                httpClient,
                graphApiVersion,
                request.ExternalAccountId,
                request.AccessToken,
                new Dictionary<string, string>
                {
                    ["image_url"] = item.SourceUrl,
                    ["is_carousel_item"] = "true",
                },
                cancellationToken);
            if (childResult.IsFailure)
            {
                return Result<OrganicPublishReceipt>.Failure(childResult.Error);
            }

            childIds.Add(childResult.Value);
        }

        var carouselResult = await CreateInstagramMediaContainerAsync(
            httpClient,
            graphApiVersion,
            request.ExternalAccountId,
            request.AccessToken,
            new Dictionary<string, string>
            {
                ["media_type"] = "CAROUSEL",
                ["children"] = string.Join(",", childIds),
                ["caption"] = request.Caption ?? string.Empty,
            },
            cancellationToken);
        if (carouselResult.IsFailure)
        {
            return Result<OrganicPublishReceipt>.Failure(carouselResult.Error);
        }

        return await PublishInstagramContainerAsync(
            httpClient,
            graphApiVersion,
            request.ExternalAccountId,
            request.AccessToken,
            carouselResult.Value,
            cancellationToken);
    }

    private static async Task<Result<OrganicPublishReceipt>> PublishInstagramReelAsync(
        HttpClient httpClient,
        string graphApiVersion,
        OrganicPublishRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Media.Count == 0 || string.IsNullOrWhiteSpace(request.Media[0].SourceUrl))
        {
            return Result<OrganicPublishReceipt>.Failure(SocialErrors.PublishFailed);
        }

        var containerResult = await CreateInstagramMediaContainerAsync(
            httpClient,
            graphApiVersion,
            request.ExternalAccountId,
            request.AccessToken,
            new Dictionary<string, string>
            {
                ["media_type"] = "REELS",
                ["video_url"] = request.Media[0].SourceUrl,
                ["caption"] = request.Caption ?? string.Empty,
            },
            cancellationToken);
        if (containerResult.IsFailure)
        {
            return Result<OrganicPublishReceipt>.Failure(containerResult.Error);
        }

        var statusUrl =
            $"https://graph.facebook.com/{graphApiVersion}/{Uri.EscapeDataString(containerResult.Value)}" +
            "?fields=status_code" +
            $"&access_token={Uri.EscapeDataString(request.AccessToken)}";

        using var statusRequest = new HttpRequestMessage(HttpMethod.Get, statusUrl);
        using var statusResponse = await httpClient.SendAsync(statusRequest, cancellationToken);
        var statusResult = await MapMetaResponseAsync(statusResponse, cancellationToken);
        if (statusResult.IsFailure)
        {
            return Result<OrganicPublishReceipt>.Failure(statusResult.Error);
        }

        using var statusJson = JsonDocument.Parse(statusResult.Value);
        var statusCode = statusJson.RootElement.TryGetProperty("status_code", out var statusProp)
            ? statusProp.GetString()
            : null;
        if (!string.Equals(statusCode, "FINISHED", StringComparison.OrdinalIgnoreCase))
        {
            return Result<OrganicPublishReceipt>.Failure(SocialErrors.PublishFailed);
        }

        return await PublishInstagramContainerAsync(
            httpClient,
            graphApiVersion,
            request.ExternalAccountId,
            request.AccessToken,
            containerResult.Value,
            cancellationToken);
    }

    private static async Task<Result<string>> CreateInstagramMediaContainerAsync(
        HttpClient httpClient,
        string graphApiVersion,
        string igUserId,
        string accessToken,
        Dictionary<string, string> fields,
        CancellationToken cancellationToken)
    {
        fields["access_token"] = accessToken;
        var url = $"https://graph.facebook.com/{graphApiVersion}/{Uri.EscapeDataString(igUserId)}/media";
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new FormUrlEncodedContent(fields),
        };

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        var mapped = await MapMetaResponseAsync(response, cancellationToken);
        if (mapped.IsFailure)
        {
            return Result<string>.Failure(mapped.Error);
        }

        using var json = JsonDocument.Parse(mapped.Value);
        if (!json.RootElement.TryGetProperty("id", out var idProp) ||
            string.IsNullOrWhiteSpace(idProp.GetString()))
        {
            return Result<string>.Failure(SocialErrors.PublishFailed);
        }

        return Result<string>.Success(idProp.GetString()!);
    }

    private static async Task<Result<OrganicPublishReceipt>> PublishInstagramContainerAsync(
        HttpClient httpClient,
        string graphApiVersion,
        string igUserId,
        string accessToken,
        string creationId,
        CancellationToken cancellationToken)
    {
        var url = $"https://graph.facebook.com/{graphApiVersion}/{Uri.EscapeDataString(igUserId)}/media_publish";
        var form = new Dictionary<string, string>
        {
            ["creation_id"] = creationId,
            ["access_token"] = accessToken,
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new FormUrlEncodedContent(form),
        };

        return await SendMetaPublishAsync(httpClient, httpRequest, cancellationToken);
    }

    private static async Task<Result<OrganicPublishReceipt>> SendMetaPublishAsync(
        HttpClient httpClient,
        HttpRequestMessage httpRequest,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
            var mapped = await MapMetaResponseAsync(response, cancellationToken);
            if (mapped.IsFailure)
            {
                return Result<OrganicPublishReceipt>.Failure(mapped.Error);
            }

            using var json = JsonDocument.Parse(mapped.Value);
            if (!json.RootElement.TryGetProperty("id", out var idProp) ||
                string.IsNullOrWhiteSpace(idProp.GetString()))
            {
                return Result<OrganicPublishReceipt>.Failure(SocialErrors.PublishFailed);
            }

            return Result<OrganicPublishReceipt>.Success(new OrganicPublishReceipt(idProp.GetString()!));
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

    private static async Task<Result<string>> MapMetaResponseAsync(
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
