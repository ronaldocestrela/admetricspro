using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Domain.Campaigns;

/// <summary>
/// Entidade de domínio universal que representa um anúncio / criativo (Ad / Creative).
/// Armazena textos publicitários, formatos e URLs de destino para auditoria e inteligência analítica.
/// </summary>
public sealed class Ad : Entity<Guid>
{
    private Ad(
        Guid id,
        Guid adSetId,
        Guid campaignId,
        Guid connectedAdAccountId,
        string externalAdId,
        string name,
        AdStatus status,
        AdCreativeType creativeType,
        string? headline,
        string? body,
        string? destinationUrl,
        string? previewUrl,
        string? callToAction,
        DateTime lastSyncedAtUtc,
        DateTime createdAtUtc,
        DateTime? updatedAtUtc)
        : base(id)
    {
        AdSetId = adSetId;
        CampaignId = campaignId;
        ConnectedAdAccountId = connectedAdAccountId;
        ExternalAdId = externalAdId;
        Name = name;
        Status = status;
        CreativeType = creativeType;
        Headline = headline;
        Body = body;
        DestinationUrl = destinationUrl;
        PreviewUrl = previewUrl;
        CallToAction = callToAction;
        LastSyncedAtUtc = lastSyncedAtUtc;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    private Ad()
        : base(Guid.Empty)
    {
        ExternalAdId = string.Empty;
        Name = string.Empty;
        Status = AdStatus.Unknown;
        CreativeType = AdCreativeType.Unknown;
    }

    /// <summary>
    /// Obtém o identificador único do conjunto pai (AdSet) ao qual o anúncio pertence.
    /// </summary>
    public Guid AdSetId { get; private set; }

    /// <summary>
    /// Obtém o identificador único da campanha associada.
    /// </summary>
    public Guid CampaignId { get; private set; }

    /// <summary>
    /// Obtém o identificador da conta de anúncios conectada associada.
    /// </summary>
    public Guid ConnectedAdAccountId { get; private set; }

    /// <summary>
    /// Obtém o identificador nativo do anúncio na rede externa (ex: ad_987654321).
    /// </summary>
    public string ExternalAdId { get; private set; }

    /// <summary>
    /// Obtém o nome descritivo do anúncio.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Obtém o status operacional ou de revisão do anúncio.
    /// </summary>
    public AdStatus Status { get; private set; }

    /// <summary>
    /// Obtém o formato de criativo do anúncio (Image, Video, Carousel, ResponsiveSearch, etc.).
    /// </summary>
    public AdCreativeType CreativeType { get; private set; }

    /// <summary>
    /// Obtém o título principal ou manchete do anúncio (Headline).
    /// </summary>
    public string? Headline { get; private set; }

    /// <summary>
    /// Obtém o texto principal / corpo de copy do anúncio.
    /// </summary>
    public string? Body { get; private set; }

    /// <summary>
    /// Obtém a URL de destino final configurada no anúncio (Landing Page URL).
    /// Essencial para monitoramento e validação de páginas ativas (Health Check).
    /// </summary>
    public string? DestinationUrl { get; private set; }

    /// <summary>
    /// Obtém a URL de pré-visualização ou miniatura do criativo (Thumbnail / Preview).
    /// </summary>
    public string? PreviewUrl { get; private set; }

    /// <summary>
    /// Obtém o botão de chamada para ação configurado (Call To Action - ex: SHOP_NOW, LEARN_MORE).
    /// </summary>
    public string? CallToAction { get; private set; }

    /// <summary>
    /// Obtém o carimbo de data/hora UTC da última sincronização bem-sucedida.
    /// </summary>
    public DateTime LastSyncedAtUtc { get; private set; }

    /// <summary>
    /// Obtém o carimbo de data/hora UTC em que o registro foi criado localmente.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Obtém o carimbo de data/hora UTC da última modificação cadastral.
    /// </summary>
    public DateTime? UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Cria uma nova instância de <see cref="Ad"/> validando invariantes de domínio.
    /// </summary>
    /// <param name="id">Identificador único do anúncio.</param>
    /// <param name="adSetId">Identificador do conjunto pai.</param>
    /// <param name="campaignId">Identificador da campanha associada.</param>
    /// <param name="connectedAdAccountId">Identificador da conta conectada.</param>
    /// <param name="externalAdId">Identificador externo na plataforma de origem.</param>
    /// <param name="name">Nome amigável do anúncio.</param>
    /// <param name="status">Status operacional unificado.</param>
    /// <param name="creativeType">Formato do criativo.</param>
    /// <param name="headline">Título ou manchete opcional.</param>
    /// <param name="body">Texto de copy opcional.</param>
    /// <param name="destinationUrl">URL de destino final opcional.</param>
    /// <param name="previewUrl">URL de pré-visualização opcional.</param>
    /// <param name="callToAction">Ação de clique opcional.</param>
    /// <param name="lastSyncedAtUtc">Carimbo UTC da sincronização.</param>
    /// <returns>Resultado contendo a entidade ou erro de validação.</returns>
    public static Result<Ad> Create(
        Guid id,
        Guid adSetId,
        Guid campaignId,
        Guid connectedAdAccountId,
        string externalAdId,
        string name,
        AdStatus status,
        AdCreativeType creativeType,
        string? headline = null,
        string? body = null,
        string? destinationUrl = null,
        string? previewUrl = null,
        string? callToAction = null,
        DateTime? lastSyncedAtUtc = null)
    {
        if (id == Guid.Empty)
        {
            return Result<Ad>.Failure(
                Error.Validation("Ad.EmptyId", "O identificador do anúncio não pode ser vazio."));
        }

        if (adSetId == Guid.Empty)
        {
            return Result<Ad>.Failure(
                Error.Validation("Ad.EmptyAdSetId", "O conjunto associado é obrigatório."));
        }

        if (campaignId == Guid.Empty)
        {
            return Result<Ad>.Failure(
                Error.Validation("Ad.EmptyCampaignId", "A campanha associada é obrigatória."));
        }

        if (connectedAdAccountId == Guid.Empty)
        {
            return Result<Ad>.Failure(
                Error.Validation("Ad.EmptyConnectedAdAccountId", "A conta de anúncios conectada é obrigatória."));
        }

        if (string.IsNullOrWhiteSpace(externalAdId))
        {
            return Result<Ad>.Failure(
                Error.Validation("Ad.EmptyExternalAdId", "O identificador externo do anúncio é obrigatório."));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<Ad>.Failure(
                Error.Validation("Ad.EmptyName", "O nome do anúncio não pode ser vazio."));
        }

        var now = DateTime.UtcNow;
        var syncTime = lastSyncedAtUtc ?? now;

        var ad = new Ad(
            id,
            adSetId,
            campaignId,
            connectedAdAccountId,
            externalAdId.Trim(),
            name.Trim(),
            status,
            creativeType,
            headline?.Trim(),
            body?.Trim(),
            destinationUrl?.Trim(),
            previewUrl?.Trim(),
            callToAction?.Trim(),
            syncTime,
            now,
            null);

        return Result<Ad>.Success(ad);
    }

    /// <summary>
    /// Atualiza os detalhes e criativo do anúncio a partir de nova sincronização.
    /// </summary>
    /// <param name="name">Novo nome.</param>
    /// <param name="status">Novo status.</param>
    /// <param name="creativeType">Formato do criativo.</param>
    /// <param name="headline">Título.</param>
    /// <param name="body">Texto de copy.</param>
    /// <param name="destinationUrl">URL de destino.</param>
    /// <param name="previewUrl">URL de prévia.</param>
    /// <param name="callToAction">Chamada para ação.</param>
    /// <param name="lastSyncedAtUtc">Carimbo UTC da sincronização.</param>
    /// <returns>Resultado da operação.</returns>
    public Result UpdateDetails(
        string name,
        AdStatus status,
        AdCreativeType creativeType,
        string? headline,
        string? body,
        string? destinationUrl,
        string? previewUrl,
        string? callToAction,
        DateTime lastSyncedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(Error.Validation("Ad.EmptyName", "O nome do anúncio não pode ser vazio."));
        }

        Name = name.Trim();
        Status = status;
        CreativeType = creativeType;
        Headline = headline?.Trim();
        Body = body?.Trim();
        DestinationUrl = destinationUrl?.Trim();
        PreviewUrl = previewUrl?.Trim();
        CallToAction = callToAction?.Trim();
        LastSyncedAtUtc = lastSyncedAtUtc;
        UpdatedAtUtc = DateTime.UtcNow;

        return Result.Success();
    }
}
