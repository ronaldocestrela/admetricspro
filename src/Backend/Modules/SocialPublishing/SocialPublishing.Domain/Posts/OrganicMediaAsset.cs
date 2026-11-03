using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;

namespace SocialPublishing.Domain.Posts;

/// <summary>
/// Entidade que descreve um item de mídia orgânica (metadados e referência ao objeto armazenado).
/// </summary>
public sealed class OrganicMediaAsset : Entity<Guid>
{
    private OrganicMediaAsset(
        Guid id,
        int width,
        int height,
        int sortOrder,
        Guid? workspaceId,
        OrganicPostFormat? format,
        string? objectKey,
        string? mimeType,
        int? durationMilliseconds,
        string? aspectLabel)
        : base(id)
    {
        Width = width;
        Height = height;
        SortOrder = sortOrder;
        WorkspaceId = workspaceId;
        Format = format;
        ObjectKey = objectKey;
        MimeType = mimeType;
        DurationMilliseconds = durationMilliseconds;
        AspectLabel = aspectLabel;
    }

    private OrganicMediaAsset()
        : base(Guid.Empty)
    {
    }

    /// <summary>
    /// Obtém a largura em pixels.
    /// </summary>
    public int Width { get; private set; }

    /// <summary>
    /// Obtém a altura em pixels.
    /// </summary>
    public int Height { get; private set; }

    /// <summary>
    /// Obtém a ordem do item no carrossel ou zero para mídia única.
    /// </summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// Obtém o workspace proprietário quando a mídia foi registrada via upload standalone.
    /// </summary>
    public Guid? WorkspaceId { get; private set; }

    /// <summary>
    /// Obtém o formato de publicação associado ao upload standalone.
    /// </summary>
    public OrganicPostFormat? Format { get; private set; }

    /// <summary>
    /// Obtém a chave do objeto no armazenamento externo (nunca o binário).
    /// </summary>
    public string? ObjectKey { get; private set; }

    /// <summary>
    /// Obtém o tipo MIME persistido.
    /// </summary>
    public string? MimeType { get; private set; }

    /// <summary>
    /// Obtém a duração em milissegundos para vídeos; nulo para imagens.
    /// </summary>
    public int? DurationMilliseconds { get; private set; }

    /// <summary>
    /// Obtém o rótulo de proporção classificado (<see cref="AspectRatioRules"/>).
    /// </summary>
    public string? AspectLabel { get; private set; }

    /// <summary>
    /// Cria um item de mídia filho de post com dimensões validadas (sem chave de objeto).
    /// </summary>
    /// <param name="width">Largura em pixels.</param>
    /// <param name="height">Altura em pixels.</param>
    /// <param name="sortOrder">Ordem no carrossel.</param>
    /// <returns>Resultado com o ativo ou erro de validação.</returns>
    public static Result<OrganicMediaAsset> Create(int width, int height, int sortOrder = 0)
    {
        var aspectResult = AspectRatio.From(width, height);
        if (aspectResult.IsFailure)
        {
            return Result<OrganicMediaAsset>.Failure(aspectResult.Error);
        }

        return Result<OrganicMediaAsset>.Success(
            new OrganicMediaAsset(
                Guid.NewGuid(),
                width,
                height,
                sortOrder,
                workspaceId: null,
                format: null,
                objectKey: null,
                mimeType: null,
                durationMilliseconds: null,
                aspectLabel: null));
    }

    /// <summary>
    /// Registra mídia standalone com metadados completos após validação de domínio.
    /// </summary>
    /// <param name="workspaceId">Workspace proprietário.</param>
    /// <param name="format">Formato de publicação alvo.</param>
    /// <param name="mimeType">Tipo MIME do arquivo.</param>
    /// <param name="durationMilliseconds">Duração em ms para vídeo; nulo para imagem.</param>
    /// <param name="width">Largura em pixels.</param>
    /// <param name="height">Altura em pixels.</param>
    /// <param name="objectKey">Chave retornada pela porta de armazenamento.</param>
    /// <param name="carouselAnchorAspectLabel">Rótulo da mídia âncora do carrossel, se aplicável.</param>
    /// <param name="assetId">Identificador fixo do ativo; quando omitido, um novo GUID é gerado.</param>
    /// <returns>Ativo registrado ou erro de negócio.</returns>
    public static Result<OrganicMediaAsset> Register(
        Guid workspaceId,
        OrganicPostFormat format,
        string mimeType,
        int? durationMilliseconds,
        int width,
        int height,
        string objectKey,
        string? carouselAnchorAspectLabel = null,
        Guid? assetId = null)
    {
        if (workspaceId == Guid.Empty)
        {
            return Result<OrganicMediaAsset>.Failure(SocialErrors.WorkspaceRequired);
        }

        if (string.IsNullOrWhiteSpace(objectKey))
        {
            return Result<OrganicMediaAsset>.Failure(
                Error.Validation("Social.MediaObjectKeyRequired", "A chave do objeto de mídia é obrigatória."));
        }

        if (!OrganicMediaMimeTypes.IsAllowed(mimeType))
        {
            return Result<OrganicMediaAsset>.Failure(SocialErrors.MediaMimeInvalid);
        }

        var mimeFormatResult = ValidateMimeForFormat(format, mimeType);
        if (mimeFormatResult.IsFailure)
        {
            return Result<OrganicMediaAsset>.Failure(mimeFormatResult.Error);
        }

        if (OrganicMediaMimeTypes.IsVideo(mimeType))
        {
            if (durationMilliseconds is null or <= 0)
            {
                return Result<OrganicMediaAsset>.Failure(
                    Error.Validation("Social.MediaDurationRequired", "A duração do vídeo deve ser positiva."));
            }
        }
        else if (durationMilliseconds is > 0)
        {
            return Result<OrganicMediaAsset>.Failure(SocialErrors.MediaMimeInvalid);
        }

        var classifyResult = AspectRatioRules.Classify(width, height);
        if (classifyResult.IsFailure)
        {
            return Result<OrganicMediaAsset>.Failure(classifyResult.Error);
        }

        var aspectLabel = classifyResult.Value;
        var formatAspectResult = AspectRatioRules.ValidateForFormat(format, aspectLabel);
        if (formatAspectResult.IsFailure)
        {
            return Result<OrganicMediaAsset>.Failure(formatAspectResult.Error);
        }

        if (format == OrganicPostFormat.FeedCarousel)
        {
            var carouselResult = AspectRatioRules.ValidateCarouselItem(carouselAnchorAspectLabel, aspectLabel);
            if (carouselResult.IsFailure)
            {
                return Result<OrganicMediaAsset>.Failure(carouselResult.Error);
            }
        }

        return Result<OrganicMediaAsset>.Success(
            new OrganicMediaAsset(
                assetId ?? Guid.NewGuid(),
                width,
                height,
                sortOrder: 0,
                workspaceId,
                format,
                objectKey.Trim(),
                mimeType,
                durationMilliseconds,
                aspectLabel));
    }

    /// <summary>
    /// Obtém a proporção derivada das dimensões.
    /// </summary>
    /// <returns>Proporção largura/altura.</returns>
    public Result<AspectRatio> GetAspectRatio() => AspectRatio.From(Width, Height);

    private static Result ValidateMimeForFormat(OrganicPostFormat format, string mimeType)
    {
        var requiresImage = format is OrganicPostFormat.FeedImage or OrganicPostFormat.FeedCarousel;
        var requiresVideo = format is OrganicPostFormat.Reel or OrganicPostFormat.TikTokVideo;

        if (requiresImage && !OrganicMediaMimeTypes.IsImage(mimeType))
        {
            return Result.Failure(SocialErrors.MediaMimeInvalid);
        }

        if (requiresVideo && !OrganicMediaMimeTypes.IsVideo(mimeType))
        {
            return Result.Failure(SocialErrors.MediaMimeInvalid);
        }

        return Result.Success();
    }
}
