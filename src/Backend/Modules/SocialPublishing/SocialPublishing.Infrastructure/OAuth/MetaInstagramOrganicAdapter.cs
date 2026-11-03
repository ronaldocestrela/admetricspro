using BuildingBlocks.Domain.Primitives;
using Microsoft.Extensions.Options;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Connections;
using SocialPublishing.Application.Options;
using SocialPublishing.Application.Publishing;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Posts;

namespace SocialPublishing.Infrastructure.OAuth;

/// <summary>
/// Adaptador OAuth orgânico para Instagram Professional/Business via Facebook Login.
/// </summary>
public sealed class MetaInstagramOrganicAdapter : IOrganicPlatformAdapter
{
    private readonly HttpClient _httpClient;
    private readonly MetaOrganicOAuthOptions _options;

    /// <summary>
    /// Inicializa o adaptador com cliente HTTP dedicado (sem loggers de URL).
    /// </summary>
    /// <param name="httpClient">Cliente HTTP nomeado do módulo.</param>
    /// <param name="options">Opções OAuth orgânicas da Meta.</param>
    public MetaInstagramOrganicAdapter(HttpClient httpClient, IOptions<OrganicOAuthOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value.Meta ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public OrganicPlatform Platform => OrganicPlatform.Instagram;

    /// <inheritdoc />
    public Result<string> BuildAuthorizationUrl(string state, string redirectUri)
    {
        if (string.IsNullOrWhiteSpace(_options.AppId))
        {
            return Result<string>.Failure(SocialErrors.OAuthConfigurationMissing);
        }

        var url =
            $"https://www.facebook.com/{_options.GraphApiVersion}/dialog/oauth?client_id={Uri.EscapeDataString(_options.AppId)}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}&state={Uri.EscapeDataString(state)}" +
            $"&scope={Uri.EscapeDataString(_options.InstagramScopes)}&response_type=code";

        return Result<string>.Success(url);
    }

    /// <inheritdoc />
    public Task<Result<OrganicAuthorizationGrant>> ExchangeCodeAsync(
        string code,
        string redirectUri,
        CancellationToken cancellationToken = default) =>
        MetaOrganicOAuthExchange.ExchangeInstagramAsync(
            _httpClient,
            _options,
            code,
            redirectUri,
            _options.InstagramScopes,
            cancellationToken);

    /// <inheritdoc />
    public Task<Result<OrganicPublishReceipt>> PublishAsync(
        OrganicPublishRequest request,
        CancellationToken cancellationToken = default) =>
        request.Format is OrganicPostFormat.FeedCarousel or OrganicPostFormat.Reel
            ? MetaOrganicPublishHttp.PublishInstagramAsync(
                _httpClient,
                _options.GraphApiVersion,
                request,
                cancellationToken)
            : Task.FromResult(Result<OrganicPublishReceipt>.Failure(SocialErrors.PublishFormatUnsupported));
}
