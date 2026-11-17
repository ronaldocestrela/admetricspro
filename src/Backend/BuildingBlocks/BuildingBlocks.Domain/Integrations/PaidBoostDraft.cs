using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Domain.Integrations;

/// <summary>
/// Rascunho local pausado de anúncio pago originado de um post orgânico com tração.
/// Não representa um objeto publicado na rede de anúncios.
/// </summary>
public sealed class PaidBoostDraft : Entity<Guid>
{
    private PaidBoostDraft(
        Guid id,
        Guid workspaceId,
        Guid suggestionId,
        Guid organicPostId,
        Guid connectedAdAccountId,
        string platform,
        PaidBoostDraftStatus status,
        string body,
        string? mediaObjectKey,
        string externalAdId,
        DateTime createdAtUtc)
        : base(id)
    {
        WorkspaceId = workspaceId;
        SuggestionId = suggestionId;
        OrganicPostId = organicPostId;
        ConnectedAdAccountId = connectedAdAccountId;
        Platform = platform;
        Status = status;
        Body = body;
        MediaObjectKey = mediaObjectKey;
        ExternalAdId = externalAdId;
        CreatedAtUtc = createdAtUtc;
    }

    private PaidBoostDraft()
        : base(Guid.Empty)
    {
        Platform = string.Empty;
        Body = string.Empty;
        ExternalAdId = string.Empty;
    }

    /// <summary>
    /// Obtém o workspace proprietário.
    /// </summary>
    public Guid WorkspaceId { get; private set; }

    /// <summary>
    /// Obtém a sugestão social que originou o rascunho.
    /// </summary>
    public Guid SuggestionId { get; private set; }

    /// <summary>
    /// Obtém o post orgânico de origem.
    /// </summary>
    public Guid OrganicPostId { get; private set; }

    /// <summary>
    /// Obtém a conta de anúncios conectada selecionada para o rascunho.
    /// </summary>
    public Guid ConnectedAdAccountId { get; private set; }

    /// <summary>
    /// Obtém a plataforma de mídia paga (MetaAds ou TikTokAds).
    /// </summary>
    public string Platform { get; private set; }

    /// <summary>
    /// Obtém o status local do rascunho.
    /// </summary>
    public PaidBoostDraftStatus Status { get; private set; }

    /// <summary>
    /// Obtém o texto principal derivado da legenda orgânica.
    /// </summary>
    public string Body { get; private set; }

    /// <summary>
    /// Obtém a chave de mídia orgânica usada como referência criativa.
    /// </summary>
    public string? MediaObjectKey { get; private set; }

    /// <summary>
    /// Obtém o identificador externo na rede; vazio enquanto o rascunho permanece apenas local.
    /// </summary>
    public string ExternalAdId { get; private set; }

    /// <summary>
    /// Obtém o instante UTC de criação do rascunho.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Cria um rascunho pausado sem escrita externa na rede de anúncios.
    /// </summary>
    /// <param name="id">Identificador do rascunho.</param>
    /// <param name="workspaceId">Workspace proprietário.</param>
    /// <param name="suggestionId">Sugestão social aceita.</param>
    /// <param name="organicPostId">Post orgânico de origem.</param>
    /// <param name="connectedAdAccountId">Conta de anúncios selecionada.</param>
    /// <param name="platform">Plataforma de mídia paga.</param>
    /// <param name="body">Legenda ou copy inicial.</param>
    /// <param name="mediaObjectKey">Chave de mídia orgânica, se houver.</param>
    /// <param name="createdAtUtc">Instante UTC de criação.</param>
    /// <returns>Rascunho pausado ou erro de validação.</returns>
    public static Result<PaidBoostDraft> CreatePaused(
        Guid id,
        Guid workspaceId,
        Guid suggestionId,
        Guid organicPostId,
        Guid connectedAdAccountId,
        string platform,
        string body,
        string? mediaObjectKey,
        DateTime createdAtUtc)
    {
        if (id == Guid.Empty || workspaceId == Guid.Empty || suggestionId == Guid.Empty || organicPostId == Guid.Empty ||
            connectedAdAccountId == Guid.Empty)
        {
            return Result<PaidBoostDraft>.Failure(
                Error.Validation("PaidBoostDraft.InvalidContext", "Contexto inválido para criar rascunho de impulsionamento."));
        }

        if (string.IsNullOrWhiteSpace(platform))
        {
            return Result<PaidBoostDraft>.Failure(
                Error.Validation("PaidBoostDraft.InvalidPlatform", "A plataforma de mídia paga é obrigatória."));
        }

        if (createdAtUtc.Kind != DateTimeKind.Utc)
        {
            return Result<PaidBoostDraft>.Failure(
                Error.Validation("PaidBoostDraft.InvalidTimestamp", "O instante de criação deve ser UTC."));
        }

        return Result<PaidBoostDraft>.Success(
            new PaidBoostDraft(
                id,
                workspaceId,
                suggestionId,
                organicPostId,
                connectedAdAccountId,
                platform.Trim(),
                PaidBoostDraftStatus.Paused,
                body ?? string.Empty,
                mediaObjectKey,
                externalAdId: string.Empty,
                createdAtUtc));
    }
}
