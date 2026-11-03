using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using MediatR;
using Microsoft.Extensions.Options;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Media.DTOs;
using SocialPublishing.Application.Options;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Posts;

namespace SocialPublishing.Application.Media.Commands.RegisterOrganicMedia;

/// <summary>
/// Handler que valida mídia orgânica antes de promover o objeto ao armazenamento definitivo.
/// </summary>
public sealed class RegisterOrganicMediaCommandHandler
    : IRequestHandler<RegisterOrganicMediaCommand, Result<OrganicMediaAssetDto>>
{
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly IOrganicMediaStore _mediaStore;
    private readonly IOrganicMediaMetadataReader _metadataReader;
    private readonly IOrganicMediaAssetRepository _assetRepository;
    private readonly ISocialPublishingUnitOfWork _unitOfWork;
    private readonly OrganicMediaOptions _options;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="RegisterOrganicMediaCommandHandler"/>.
    /// </summary>
    public RegisterOrganicMediaCommandHandler(
        ITenantContextAccessor tenantContextAccessor,
        IOrganicMediaStore mediaStore,
        IOrganicMediaMetadataReader metadataReader,
        IOrganicMediaAssetRepository assetRepository,
        ISocialPublishingUnitOfWork unitOfWork,
        IOptions<OrganicMediaOptions> options)
    {
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
        _mediaStore = mediaStore ?? throw new ArgumentNullException(nameof(mediaStore));
        _metadataReader = metadataReader ?? throw new ArgumentNullException(nameof(metadataReader));
        _assetRepository = assetRepository ?? throw new ArgumentNullException(nameof(assetRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public async Task<Result<OrganicMediaAssetDto>> Handle(
        RegisterOrganicMediaCommand command,
        CancellationToken cancellationToken)
    {
        if (command.WorkspaceId == Guid.Empty)
        {
            return Result<OrganicMediaAssetDto>.Failure(SocialErrors.WorkspaceRequired);
        }

        var tenantId = _tenantContextAccessor.TenantContext?.TenantId;
        if (tenantId is null || tenantId == Guid.Empty)
        {
            return Result<OrganicMediaAssetDto>.Failure(SocialErrors.TenantRequired);
        }

        if (command.Content.CanSeek && command.Content.Length > _options.MaxUploadBytes)
        {
            return Result<OrganicMediaAssetDto>.Failure(
                Error.Validation("Social.MediaTooLarge", "O arquivo excede o tamanho máximo permitido."));
        }

        var assetId = Guid.NewGuid();
        string? stagingKey = null;
        string? durableKey = null;

        try
        {
            var stagingSave = await _mediaStore.SaveAsync(
                tenantId.Value,
                command.WorkspaceId,
                assetId,
                command.Content,
                command.MimeType,
                OrganicMediaStorageTier.Staging,
                cancellationToken);

            if (stagingSave.IsFailure)
            {
                return Result<OrganicMediaAssetDto>.Failure(stagingSave.Error);
            }

            stagingKey = stagingSave.Value;

            var openStaging = await _mediaStore.OpenReadAsync(stagingKey, cancellationToken);
            if (openStaging.IsFailure)
            {
                await TryDeleteAsync(stagingKey, cancellationToken);
                return Result<OrganicMediaAssetDto>.Failure(openStaging.Error);
            }

            await using (openStaging.Value)
            {
                var metadataResult = await _metadataReader.ReadAsync(openStaging.Value, command.MimeType, cancellationToken);
                if (metadataResult.IsFailure)
                {
                    await TryDeleteAsync(stagingKey, cancellationToken);
                    return Result<OrganicMediaAssetDto>.Failure(metadataResult.Error);
                }

                var metadata = metadataResult.Value;
                var anchorLabel = await ResolveCarouselAnchorLabelAsync(command, cancellationToken);
                if (anchorLabel.IsFailure)
                {
                    await TryDeleteAsync(stagingKey, cancellationToken);
                    return Result<OrganicMediaAssetDto>.Failure(anchorLabel.Error);
                }

                var validation = OrganicMediaAsset.Register(
                    command.WorkspaceId,
                    command.Format,
                    command.MimeType,
                    metadata.DurationMilliseconds,
                    metadata.Width,
                    metadata.Height,
                    stagingKey,
                    anchorLabel.Value,
                    assetId);

                if (validation.IsFailure)
                {
                    await TryDeleteAsync(stagingKey, cancellationToken);
                    return Result<OrganicMediaAssetDto>.Failure(validation.Error);
                }
            }

            var reopen = await _mediaStore.OpenReadAsync(stagingKey, cancellationToken);
            if (reopen.IsFailure)
            {
                await TryDeleteAsync(stagingKey, cancellationToken);
                return Result<OrganicMediaAssetDto>.Failure(reopen.Error);
            }

            await using (reopen.Value)
            {
                var durableSave = await _mediaStore.SaveAsync(
                    tenantId.Value,
                    command.WorkspaceId,
                    assetId,
                    reopen.Value,
                    command.MimeType,
                    OrganicMediaStorageTier.Durable,
                    cancellationToken);

                if (durableSave.IsFailure)
                {
                    await TryDeleteAsync(stagingKey, cancellationToken);
                    return Result<OrganicMediaAssetDto>.Failure(durableSave.Error);
                }

                durableKey = durableSave.Value;
            }

            await TryDeleteAsync(stagingKey, cancellationToken);
            stagingKey = null;

            var anchorLabelFinal = await ResolveCarouselAnchorLabelAsync(command, cancellationToken);
            if (anchorLabelFinal.IsFailure)
            {
                await TryDeleteAsync(durableKey, cancellationToken);
                return Result<OrganicMediaAssetDto>.Failure(anchorLabelFinal.Error);
            }

            var openDurable = await _mediaStore.OpenReadAsync(durableKey, cancellationToken);
            if (openDurable.IsFailure)
            {
                await TryDeleteAsync(durableKey, cancellationToken);
                return Result<OrganicMediaAssetDto>.Failure(openDurable.Error);
            }

            OrganicMediaMetadata metadataFinal;
            await using (openDurable.Value)
            {
                var metadataResult = await _metadataReader.ReadAsync(openDurable.Value, command.MimeType, cancellationToken);
                if (metadataResult.IsFailure)
                {
                    await TryDeleteAsync(durableKey, cancellationToken);
                    return Result<OrganicMediaAssetDto>.Failure(metadataResult.Error);
                }

                metadataFinal = metadataResult.Value;
            }

            var assetResult = OrganicMediaAsset.Register(
                command.WorkspaceId,
                command.Format,
                command.MimeType,
                metadataFinal.DurationMilliseconds,
                metadataFinal.Width,
                metadataFinal.Height,
                durableKey,
                anchorLabelFinal.Value,
                assetId);

            if (assetResult.IsFailure)
            {
                await TryDeleteAsync(durableKey, cancellationToken);
                return Result<OrganicMediaAssetDto>.Failure(assetResult.Error);
            }

            await _assetRepository.AddAsync(assetResult.Value, cancellationToken);
            try
            {
                await _unitOfWork.CommitAsync(cancellationToken);
            }
            catch
            {
                await TryDeleteAsync(durableKey, cancellationToken);
                throw;
            }

            var persisted = assetResult.Value;
            return Result<OrganicMediaAssetDto>.Success(
                new OrganicMediaAssetDto(
                    persisted.Id,
                    persisted.WorkspaceId!.Value,
                    persisted.Format!.Value,
                    persisted.MimeType!,
                    persisted.Width,
                    persisted.Height,
                    persisted.DurationMilliseconds,
                    persisted.AspectLabel!,
                    persisted.ObjectKey!));
        }
        catch (Exception)
        {
            if (stagingKey is not null)
            {
                await TryDeleteAsync(stagingKey, cancellationToken);
            }

            if (durableKey is not null)
            {
                await TryDeleteAsync(durableKey, cancellationToken);
            }

            throw;
        }
    }

    private async Task<Result<string?>> ResolveCarouselAnchorLabelAsync(
        RegisterOrganicMediaCommand command,
        CancellationToken cancellationToken)
    {
        if (command.CarouselAnchorAssetId is not Guid anchorId || anchorId == Guid.Empty)
        {
            return Result<string?>.Success(null);
        }

        var anchor = await _assetRepository.GetByIdAsync(anchorId, cancellationToken);
        if (anchor is null || anchor.WorkspaceId != command.WorkspaceId)
        {
            return Result<string?>.Failure(
                Error.NotFound("Social.MediaAnchorNotFound", "A mídia âncora do carrossel não foi encontrada."));
        }

        return Result<string?>.Success(anchor.AspectLabel);
    }

    private async Task TryDeleteAsync(string objectKey, CancellationToken cancellationToken)
    {
        _ = await _mediaStore.DeleteAsync(objectKey, cancellationToken);
    }
}
