using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Domain.Brand;
using SocialPublishing.Domain.Connections;

namespace SocialPublishing.Domain.Posts;

/// <summary>
/// Agregado raiz de um post orgânico com máquina de estados, legenda, mídia e horário em UTC.
/// </summary>
public sealed class OrganicPost : AggregateRoot<Guid>
{
    private const int CarouselMinItems = 2;
    private const int CarouselMaxItems = 10;

    private readonly List<OrganicMediaAsset> _media = new();

    private OrganicPost(
        Guid id,
        Guid workspaceId,
        Guid connectionId,
        OrganicPlatform platform,
        OrganicPostFormat format,
        ContentPillar pillar,
        string caption,
        DateTime scheduledAtUtc,
        OrganicPostStatus status,
        DateTime createdAtUtc)
        : base(id)
    {
        WorkspaceId = workspaceId;
        ConnectionId = connectionId;
        Platform = platform;
        Format = format;
        Pillar = pillar;
        Caption = caption;
        ScheduledAtUtc = scheduledAtUtc;
        Status = status;
        CreatedAtUtc = createdAtUtc;
    }

    private OrganicPost()
        : base(Guid.Empty)
    {
        Caption = string.Empty;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Obtém o identificador do workspace.
    /// </summary>
    public Guid WorkspaceId { get; private set; }

    /// <summary>
    /// Obtém o identificador da conexão orgânica utilizada na publicação.
    /// </summary>
    public Guid ConnectionId { get; private set; }

    /// <summary>
    /// Obtém a plataforma de destino.
    /// </summary>
    public OrganicPlatform Platform { get; private set; }

    /// <summary>
    /// Obtém o formato do post.
    /// </summary>
    public OrganicPostFormat Format { get; private set; }

    /// <summary>
    /// Obtém o pilar editorial do conteúdo.
    /// </summary>
    public ContentPillar Pillar { get; private set; }

    /// <summary>
    /// Obtém a legenda do post.
    /// </summary>
    public string Caption { get; private set; }

    /// <summary>
    /// Obtém o horário agendado em UTC.
    /// </summary>
    public DateTime ScheduledAtUtc { get; private set; }

    /// <summary>
    /// Obtém o status atual do post.
    /// </summary>
    public OrganicPostStatus Status { get; private set; }

    /// <summary>
    /// Obtém o carimbo UTC de criação.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Obtém a coleção de mídias associadas ao post.
    /// </summary>
    public IReadOnlyList<OrganicMediaAsset> Media => _media.AsReadOnly();

    /// <summary>
    /// Cria um post orgânico em status <see cref="OrganicPostStatus.Draft"/>.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="connectionId">Conexão orgânica.</param>
    /// <param name="platform">Plataforma de destino.</param>
    /// <param name="format">Formato do post.</param>
    /// <param name="pillar">Pilar editorial.</param>
    /// <param name="caption">Legenda.</param>
    /// <param name="localScheduledAt">Data/hora local desejada no fuso do workspace.</param>
    /// <param name="workspaceTimeZoneId">Identificador IANA do fuso (ex.: America/Sao_Paulo).</param>
    /// <param name="mediaItems">Itens de mídia com dimensões.</param>
    /// <returns>Resultado com o post criado ou erro de validação.</returns>
    public static Result<OrganicPost> Create(
        Guid workspaceId,
        Guid connectionId,
        OrganicPlatform platform,
        OrganicPostFormat format,
        ContentPillar pillar,
        string caption,
        DateTime localScheduledAt,
        string workspaceTimeZoneId,
        IReadOnlyList<(int Width, int Height, int SortOrder)>? mediaItems = null)
    {
        if (workspaceId == Guid.Empty)
        {
            return Result<OrganicPost>.Failure(SocialErrors.WorkspaceRequired);
        }

        if (connectionId == Guid.Empty)
        {
            return Result<OrganicPost>.Failure(
                Error.Validation("Social.ConnectionRequired", "A conexão orgânica é obrigatória."));
        }

        var captionValidation = ValidateCaption(platform, caption);
        if (captionValidation.IsFailure)
        {
            return Result<OrganicPost>.Failure(captionValidation.Error);
        }

        var mediaValidation = ValidateMediaForFormat(format, mediaItems);
        if (mediaValidation.IsFailure)
        {
            return Result<OrganicPost>.Failure(mediaValidation.Error);
        }

        var scheduledUtcResult = ConvertLocalToUtc(localScheduledAt, workspaceTimeZoneId);
        if (scheduledUtcResult.IsFailure)
        {
            return Result<OrganicPost>.Failure(scheduledUtcResult.Error);
        }

        var post = new OrganicPost(
            Guid.NewGuid(),
            workspaceId,
            connectionId,
            platform,
            format,
            pillar,
            caption,
            scheduledUtcResult.Value,
            OrganicPostStatus.Draft,
            DateTime.UtcNow);

        if (mediaItems is not null)
        {
            foreach (var item in mediaItems.OrderBy(m => m.SortOrder))
            {
                var assetResult = OrganicMediaAsset.Create(item.Width, item.Height, item.SortOrder);
                if (assetResult.IsFailure)
                {
                    return Result<OrganicPost>.Failure(assetResult.Error);
                }

                post._media.Add(assetResult.Value);
            }
        }

        return Result<OrganicPost>.Success(post);
    }

    /// <summary>
    /// Envia o post para aprovação.
    /// </summary>
    /// <returns>Resultado da transição.</returns>
    public Result SubmitForApproval() =>
        Transition(OrganicPostStatus.Draft, OrganicPostStatus.PendingApproval);

    /// <summary>
    /// Aprova o post após revisão.
    /// </summary>
    /// <returns>Resultado da transição.</returns>
    public Result Approve() =>
        Transition(OrganicPostStatus.PendingApproval, OrganicPostStatus.Approved);

    /// <summary>
    /// Rejeita o post na aprovação.
    /// </summary>
    /// <returns>Resultado da transição.</returns>
    public Result Reject() =>
        Transition(OrganicPostStatus.PendingApproval, OrganicPostStatus.Rejected);

    /// <summary>
    /// Solicita alterações ao autor.
    /// </summary>
    /// <returns>Resultado da transição.</returns>
    public Result RequestChanges() =>
        Transition(OrganicPostStatus.PendingApproval, OrganicPostStatus.ChangesRequested);

    /// <summary>
    /// Agenda o post para publicação no horário já definido.
    /// </summary>
    /// <returns>Resultado da transição.</returns>
    public Result Schedule() =>
        Transition(OrganicPostStatus.Approved, OrganicPostStatus.Scheduled);

    /// <summary>
    /// Marca o post como em publicação na rede.
    /// </summary>
    /// <returns>Resultado da transição.</returns>
    public Result MarkPublishing() =>
        Transition(OrganicPostStatus.Scheduled, OrganicPostStatus.Publishing);

    /// <summary>
    /// Marca o post como publicado com sucesso.
    /// </summary>
    /// <returns>Resultado da transição.</returns>
    public Result MarkPublished() =>
        Transition(OrganicPostStatus.Publishing, OrganicPostStatus.Published);

    /// <summary>
    /// Marca falha definitiva após tentativas de publicação.
    /// </summary>
    /// <returns>Resultado da transição.</returns>
    public Result MarkFailed() =>
        Transition(OrganicPostStatus.Publishing, OrganicPostStatus.Failed);

    /// <summary>
    /// Cancela o post nos status permitidos.
    /// </summary>
    /// <returns>Resultado da transição.</returns>
    public Result Cancel()
    {
        return Status switch
        {
            OrganicPostStatus.Draft => Transition(OrganicPostStatus.Draft, OrganicPostStatus.Cancelled),
            OrganicPostStatus.PendingApproval => Transition(OrganicPostStatus.PendingApproval, OrganicPostStatus.Cancelled),
            OrganicPostStatus.Approved => Transition(OrganicPostStatus.Approved, OrganicPostStatus.Cancelled),
            OrganicPostStatus.Scheduled => Transition(OrganicPostStatus.Scheduled, OrganicPostStatus.Cancelled),
            OrganicPostStatus.Failed => Transition(OrganicPostStatus.Failed, OrganicPostStatus.Cancelled),
            _ => Result.Failure(SocialErrors.InvalidTransition),
        };
    }

    /// <summary>
    /// Reagenda o post convertendo horário local do workspace para UTC.
    /// </summary>
    /// <param name="localScheduledAt">Nova data/hora local.</param>
    /// <param name="workspaceTimeZoneId">Fuso IANA do workspace.</param>
    /// <returns>Resultado da operação.</returns>
    public Result Reschedule(DateTime localScheduledAt, string workspaceTimeZoneId)
    {
        if (Status is OrganicPostStatus.Publishing or OrganicPostStatus.Published or OrganicPostStatus.Cancelled)
        {
            return Result.Failure(SocialErrors.SlotNotReschedulable);
        }

        if (Status is not (OrganicPostStatus.Draft or OrganicPostStatus.PendingApproval or OrganicPostStatus.Approved or OrganicPostStatus.Scheduled))
        {
            return Result.Failure(SocialErrors.InvalidTransition);
        }

        var utcResult = ConvertLocalToUtc(localScheduledAt, workspaceTimeZoneId);
        if (utcResult.IsFailure)
        {
            return utcResult;
        }

        ScheduledAtUtc = utcResult.Value;
        return Result.Success();
    }

    private Result Transition(OrganicPostStatus expectedCurrent, OrganicPostStatus next)
    {
        if (Status != expectedCurrent)
        {
            return Result.Failure(SocialErrors.InvalidTransition);
        }

        Status = next;
        return Result.Success();
    }

    private static Result ValidateCaption(OrganicPlatform platform, string caption)
    {
        var max = CaptionLimits.GetMaxLength(platform);
        if (caption.Length > max)
        {
            return Result.Failure(SocialErrors.CaptionTooLong);
        }

        return Result.Success();
    }

    private static Result ValidateMediaForFormat(
        OrganicPostFormat format,
        IReadOnlyList<(int Width, int Height, int SortOrder)>? mediaItems)
    {
        var count = mediaItems?.Count ?? 0;

        switch (format)
        {
            case OrganicPostFormat.FeedCarousel:
                if (count < CarouselMinItems || count > CarouselMaxItems)
                {
                    return Result.Failure(SocialErrors.CarouselItemCountInvalid);
                }

                return Result.Success();

            case OrganicPostFormat.Reel:
            case OrganicPostFormat.TikTokVideo:
                if (count == 0)
                {
                    return Result.Failure(SocialErrors.MediaAspectInvalid);
                }

                foreach (var item in mediaItems!)
                {
                    var aspect = AspectRatio.From(item.Width, item.Height);
                    if (aspect.IsFailure || !aspect.Value.IsVertical916())
                    {
                        return Result.Failure(SocialErrors.MediaAspectInvalid);
                    }
                }

                return Result.Success();

            default:
                return Result.Success();
        }
    }

    private static Result<DateTime> ConvertLocalToUtc(DateTime localScheduledAt, string workspaceTimeZoneId)
    {
        if (string.IsNullOrWhiteSpace(workspaceTimeZoneId))
        {
            return Result<DateTime>.Failure(
                Error.Validation("Social.TimeZoneRequired", "O fuso horário do workspace é obrigatório."));
        }

        try
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(workspaceTimeZoneId);
            var unspecified = DateTime.SpecifyKind(localScheduledAt, DateTimeKind.Unspecified);
            var utc = TimeZoneInfo.ConvertTimeToUtc(unspecified, timeZone);
            return Result<DateTime>.Success(DateTime.SpecifyKind(utc, DateTimeKind.Utc));
        }
        catch (TimeZoneNotFoundException)
        {
            return Result<DateTime>.Failure(
                Error.Validation("Social.TimeZoneInvalid", "O fuso horário informado não é válido."));
        }
        catch (InvalidTimeZoneException)
        {
            return Result<DateTime>.Failure(
                Error.Validation("Social.TimeZoneInvalid", "O fuso horário informado não é válido."));
        }
    }
}
