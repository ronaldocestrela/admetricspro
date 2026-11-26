using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Application.Storage;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.Storage;
using Microsoft.Extensions.Options;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Media;
using SocialPublishing.Application.Options;
using SocialPublishing.Domain;

namespace SocialPublishing.Infrastructure.Media;

/// <summary>
/// Emite links HTTPS da API com token HMAC de leitura pública.
/// </summary>
public sealed class SignedOrganicMediaPublicUrlResolver : IOrganicMediaPublicUrlResolver
{
    private readonly ObjectStorageOptions _options;
    private readonly OrganicMediaOptions _mediaOptions;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly IOrganicMediaPublishTokenService _tokenService;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Inicializa o resolvedor de URL pública assinada.
    /// </summary>
    /// <param name="options">Origem HTTPS e TTL de publicação.</param>
    /// <param name="mediaOptions">Prefixo de chave orgânica.</param>
    /// <param name="tenantContextAccessor">Contexto do tenant atual.</param>
    /// <param name="tokenService">Serviço de emissão de tokens.</param>
    /// <param name="timeProvider">Relógio da aplicação.</param>
    public SignedOrganicMediaPublicUrlResolver(
        IOptions<ObjectStorageOptions> options,
        IOptions<OrganicMediaOptions> mediaOptions,
        ITenantContextAccessor tenantContextAccessor,
        IOrganicMediaPublishTokenService tokenService,
        TimeProvider timeProvider)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _mediaOptions = mediaOptions?.Value ?? throw new ArgumentNullException(nameof(mediaOptions));
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
    public Result<string> ResolvePublicUrl(string objectKey, string contentType)
    {
        if (string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
        {
            return Result<string>.Failure(SocialErrors.MediaPublicBaseUrlMissing);
        }

        if (string.IsNullOrWhiteSpace(objectKey) || string.IsNullOrWhiteSpace(contentType))
        {
            return Result<string>.Failure(StorageErrors.KeyInvalid);
        }

        var contextTenantId = _tenantContextAccessor.TenantContext?.TenantId;
        if (contextTenantId is null || contextTenantId == Guid.Empty)
        {
            return Result<string>.Failure(SocialErrors.TenantRequired);
        }

        if (!OrganicMediaObjectKeyFormatter.TryGetTenantId(objectKey, _mediaOptions.KeyPrefix, out var keyTenant)
            || keyTenant != contextTenantId.Value)
        {
            return Result<string>.Failure(StorageErrors.KeyInvalid);
        }

        var tokenResult = _tokenService.IssueToken(
            contextTenantId.Value,
            objectKey,
            contentType,
            _timeProvider.GetUtcNow().UtcDateTime);

        if (tokenResult.IsFailure)
        {
            return Result<string>.Failure(tokenResult.Error);
        }

        var baseUrl = _options.PublicBaseUrl.TrimEnd('/');
        return Result<string>.Success($"{baseUrl}/api/v1/public/media/{tokenResult.Value}");
    }
}