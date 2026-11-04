using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Domain.Connections;

namespace SocialPublishing.Domain.Traction;

/// <summary>
/// Sugestão persistida para impulsionar um post orgânico com tração anômala.
/// </summary>
public sealed class PaidBoostSuggestion : Entity<Guid>
{
    private PaidBoostSuggestion(
        Guid id,
        Guid workspaceId,
        Guid organicPostId,
        OrganicPlatform platform,
        PaidBoostSuggestionStatus status,
        long impressions,
        decimal engagementRate,
        decimal baselineMedianEngagement,
        decimal engagementMultiple,
        DateTime detectedAtUtc,
        Guid? paidDraftId)
        : base(id)
    {
        WorkspaceId = workspaceId;
        OrganicPostId = organicPostId;
        Platform = platform;
        Status = status;
        Impressions = impressions;
        EngagementRate = engagementRate;
        BaselineMedianEngagement = baselineMedianEngagement;
        EngagementMultiple = engagementMultiple;
        DetectedAtUtc = detectedAtUtc;
        PaidDraftId = paidDraftId;
    }

    private PaidBoostSuggestion()
        : base(Guid.Empty)
    {
    }

    /// <summary>
    /// Obtém o workspace proprietário.
    /// </summary>
    public Guid WorkspaceId { get; private set; }

    /// <summary>
    /// Obtém o post orgânico associado.
    /// </summary>
    public Guid OrganicPostId { get; private set; }

    /// <summary>
    /// Obtém a rede social do post.
    /// </summary>
    public OrganicPlatform Platform { get; private set; }

    /// <summary>
    /// Obtém o status atual da sugestão.
    /// </summary>
    public PaidBoostSuggestionStatus Status { get; private set; }

    /// <summary>
    /// Obtém as impressões no momento da detecção.
    /// </summary>
    public long Impressions { get; private set; }

    /// <summary>
    /// Obtém a taxa de engajamento no momento da detecção.
    /// </summary>
    public decimal EngagementRate { get; private set; }

    /// <summary>
    /// Obtém a mediana de engajamento usada como linha de base.
    /// </summary>
    public decimal BaselineMedianEngagement { get; private set; }

    /// <summary>
    /// Obtém o múltiplo de engajamento em relação à mediana.
    /// </summary>
    public decimal EngagementMultiple { get; private set; }

    /// <summary>
    /// Obtém o instante UTC em que a tração foi detectada.
    /// </summary>
    public DateTime DetectedAtUtc { get; private set; }

    /// <summary>
    /// Obtém o identificador do rascunho de anúncio pago, quando aceito.
    /// </summary>
    public Guid? PaidDraftId { get; private set; }

    /// <summary>
    /// Abre uma nova sugestão de impulsionamento.
    /// </summary>
    /// <param name="workspaceId">Workspace proprietário.</param>
    /// <param name="organicPostId">Post orgânico.</param>
    /// <param name="platform">Rede social.</param>
    /// <param name="impressions">Impressões observadas.</param>
    /// <param name="engagementRate">Taxa de engajamento.</param>
    /// <param name="baselineMedianEngagement">Mediana da janela.</param>
    /// <param name="engagementMultiple">Múltiplo sobre a mediana.</param>
    /// <param name="detectedAtUtc">Instante da detecção em UTC.</param>
    /// <returns>Sugestão em status <see cref="PaidBoostSuggestionStatus.Open"/>.</returns>
    public static Result<PaidBoostSuggestion> Open(
        Guid workspaceId,
        Guid organicPostId,
        OrganicPlatform platform,
        long impressions,
        decimal engagementRate,
        decimal baselineMedianEngagement,
        decimal engagementMultiple,
        DateTime detectedAtUtc)
    {
        if (workspaceId == Guid.Empty)
        {
            return Result<PaidBoostSuggestion>.Failure(SocialErrors.WorkspaceRequired);
        }

        if (organicPostId == Guid.Empty)
        {
            return Result<PaidBoostSuggestion>.Failure(SocialErrors.PostNotFound);
        }

        if (detectedAtUtc.Kind != DateTimeKind.Utc)
        {
            return Result<PaidBoostSuggestion>.Failure(SocialErrors.ScheduleInstantNotUtc);
        }

        return Result<PaidBoostSuggestion>.Success(
            new PaidBoostSuggestion(
                Guid.NewGuid(),
                workspaceId,
                organicPostId,
                platform,
                PaidBoostSuggestionStatus.Open,
                impressions,
                engagementRate,
                baselineMedianEngagement,
                engagementMultiple,
                detectedAtUtc,
                paidDraftId: null));
    }
}
