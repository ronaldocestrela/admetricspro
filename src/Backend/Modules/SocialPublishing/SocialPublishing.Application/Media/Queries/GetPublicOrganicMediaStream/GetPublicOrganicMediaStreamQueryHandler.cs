using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Application.Storage;
using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Domain;

namespace SocialPublishing.Application.Media.Queries.GetPublicOrganicMediaStream;

/// <summary>
/// Valida token público, fixa o tenant no contexto e abre o objeto no <see cref="IOrganicMediaStore"/>.
/// </summary>
public sealed class GetPublicOrganicMediaStreamQueryHandler
    : IQueryHandler<GetPublicOrganicMediaStreamQuery, PublicOrganicMediaStreamResult>
{
    private readonly IOrganicMediaPublishTokenService _tokenService;
    private readonly IOrganicMediaStore _mediaStore;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Inicializa o handler de stream público.
    /// </summary>
    /// <param name="tokenService">Validação HMAC do token.</param>
    /// <param name="mediaStore">Porta de leitura de mídia orgânica.</param>
    /// <param name="tenantContextAccessor">Contexto multitenant da requisição.</param>
    /// <param name="timeProvider">Relógio da aplicação.</param>
    public GetPublicOrganicMediaStreamQueryHandler(
        IOrganicMediaPublishTokenService tokenService,
        IOrganicMediaStore mediaStore,
        ITenantContextAccessor tenantContextAccessor,
        TimeProvider timeProvider)
    {
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _mediaStore = mediaStore ?? throw new ArgumentNullException(nameof(mediaStore));
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
    public async Task<Result<PublicOrganicMediaStreamResult>> Handle(
        GetPublicOrganicMediaStreamQuery request,
        CancellationToken cancellationToken)
    {
        var validated = _tokenService.ValidateToken(
            request.Token,
            _timeProvider.GetUtcNow().UtcDateTime);

        if (validated.IsFailure)
        {
            return Result<PublicOrganicMediaStreamResult>.Failure(validated.Error);
        }

        var payload = validated.Value;
        var existingTenantId = _tenantContextAccessor.TenantContext?.TenantId;
        if (existingTenantId.HasValue
            && existingTenantId.Value != Guid.Empty
            && existingTenantId.Value != payload.TenantId)
        {
            return Result<PublicOrganicMediaStreamResult>.Failure(SocialErrors.PublicMediaNotFound);
        }

        if (!existingTenantId.HasValue || existingTenantId == Guid.Empty)
        {
            _tenantContextAccessor.TenantContext = new ScopedTenantContext(
                payload.TenantId,
                TenantResolutionSource.PublishToken,
                rawIdentifier: "public-media-token");
        }

        var read = await _mediaStore.OpenReadAsync(payload.ObjectKey, cancellationToken);
        if (read.IsFailure)
        {
            if (read.Error.Code == StorageErrors.KeyInvalid.Code)
            {
                return Result<PublicOrganicMediaStreamResult>.Failure(read.Error);
            }

            return Result<PublicOrganicMediaStreamResult>.Failure(SocialErrors.PublicMediaNotFound);
        }

        return Result<PublicOrganicMediaStreamResult>.Success(
            new PublicOrganicMediaStreamResult(read.Value, payload.ContentType));
    }
}