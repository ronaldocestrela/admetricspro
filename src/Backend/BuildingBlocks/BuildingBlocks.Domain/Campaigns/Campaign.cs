using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Domain.Campaigns;

/// <summary>
/// Entidade de domínio universal que representa uma campanha de anúncios vinculada a uma conta conectada.
/// Modelo padronizado independente da plataforma de origem (Meta, Google, TikTok, Bing).
/// </summary>
public sealed class Campaign : Entity<Guid>
{
    private readonly List<AdSet> _adSets = new();

    private Campaign(
        Guid id,
        Guid workspaceId,
        Guid connectedAdAccountId,
        string platform,
        string externalCampaignId,
        string name,
        CampaignStatus status,
        string objective,
        decimal? dailyBudget,
        decimal? lifetimeBudget,
        string currency,
        DateTime? startDateUtc,
        DateTime? endDateUtc,
        DateTime lastSyncedAtUtc,
        DateTime createdAtUtc,
        DateTime? updatedAtUtc)
        : base(id)
    {
        WorkspaceId = workspaceId;
        ConnectedAdAccountId = connectedAdAccountId;
        Platform = platform;
        ExternalCampaignId = externalCampaignId;
        Name = name;
        Status = status;
        Objective = objective;
        DailyBudget = dailyBudget;
        LifetimeBudget = lifetimeBudget;
        Currency = currency;
        StartDateUtc = startDateUtc;
        EndDateUtc = endDateUtc;
        LastSyncedAtUtc = lastSyncedAtUtc;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    private Campaign()
        : base(Guid.Empty)
    {
        Platform = string.Empty;
        ExternalCampaignId = string.Empty;
        Name = string.Empty;
        Objective = string.Empty;
        Currency = "BRL";
        Status = CampaignStatus.Unknown;
    }

    /// <summary>
    /// Obtém o identificador único do workspace ao qual a campanha pertence.
    /// </summary>
    public Guid WorkspaceId { get; private set; }

    /// <summary>
    /// Obtém o identificador da conta de anúncios conectada (ConnectedAdAccount).
    /// </summary>
    public Guid ConnectedAdAccountId { get; private set; }

    /// <summary>
    /// Obtém o nome da plataforma de mídia (MetaAds, GoogleAds, TikTokAds, BingAds).
    /// </summary>
    public string Platform { get; private set; }

    /// <summary>
    /// Obtém o identificador nativo da campanha na rede de anúncios externa.
    /// </summary>
    public string ExternalCampaignId { get; private set; }

    /// <summary>
    /// Obtém o nome descritivo da campanha.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Obtém o estado operacional unificado da campanha.
    /// </summary>
    public CampaignStatus Status { get; private set; }

    /// <summary>
    /// Obtém o objetivo de marketing configurado na campanha (ex: Conversions, Traffic, Leads).
    /// </summary>
    public string Objective { get; private set; }

    /// <summary>
    /// Obtém o orçamento diário programado, se aplicável a nível de campanha.
    /// </summary>
    public decimal? DailyBudget { get; private set; }

    /// <summary>
    /// Obtém o orçamento vitalício ou total do período, se aplicável.
    /// </summary>
    public decimal? LifetimeBudget { get; private set; }

    /// <summary>
    /// Obtém o código ISO da moeda configurada na campanha (ex: BRL, USD).
    /// </summary>
    public string Currency { get; private set; }

    /// <summary>
    /// Obtém a data UTC de início da veiculação da campanha.
    /// </summary>
    public DateTime? StartDateUtc { get; private set; }

    /// <summary>
    /// Obtém a data UTC de término programado da veiculação, se houver.
    /// </summary>
    public DateTime? EndDateUtc { get; private set; }

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
    /// Obtém a coleção de conjuntos ou grupos de anúncios subordinados a esta campanha.
    /// </summary>
    public IReadOnlyCollection<AdSet> AdSets => _adSets.AsReadOnly();

    /// <summary>
    /// Cria uma nova instância de <see cref="Campaign"/> validando invariantes de domínio.
    /// </summary>
    /// <param name="id">Identificador único da campanha.</param>
    /// <param name="workspaceId">Identificador do workspace associado.</param>
    /// <param name="connectedAdAccountId">Identificador da conta conectada associada.</param>
    /// <param name="platform">Plataforma de anúncios externa.</param>
    /// <param name="externalCampaignId">Identificador externo na rede externa.</param>
    /// <param name="name">Nome amigável da campanha.</param>
    /// <param name="status">Status operacional unificado.</param>
    /// <param name="objective">Objetivo da campanha.</param>
    /// <param name="dailyBudget">Orçamento diário opcional.</param>
    /// <param name="lifetimeBudget">Orçamento total opcional.</param>
    /// <param name="currency">Moeda da campanha (padrão: BRL).</param>
    /// <param name="startDateUtc">Data de início da veiculação em UTC.</param>
    /// <param name="endDateUtc">Data de término da veiculação em UTC.</param>
    /// <param name="lastSyncedAtUtc">Carimbo UTC da sincronização.</param>
    /// <returns>Resultado contendo a campanha criada ou erro de validação.</returns>
    public static Result<Campaign> Create(
        Guid id,
        Guid workspaceId,
        Guid connectedAdAccountId,
        string platform,
        string externalCampaignId,
        string name,
        CampaignStatus status,
        string objective,
        decimal? dailyBudget = null,
        decimal? lifetimeBudget = null,
        string currency = "BRL",
        DateTime? startDateUtc = null,
        DateTime? endDateUtc = null,
        DateTime? lastSyncedAtUtc = null)
    {
        if (id == Guid.Empty)
        {
            return Result<Campaign>.Failure(
                Error.Validation("Campaign.EmptyId", "O identificador da campanha não pode ser vazio."));
        }

        if (workspaceId == Guid.Empty)
        {
            return Result<Campaign>.Failure(
                Error.Validation("Campaign.EmptyWorkspaceId", "O workspace associado é obrigatório."));
        }

        if (connectedAdAccountId == Guid.Empty)
        {
            return Result<Campaign>.Failure(
                Error.Validation("Campaign.EmptyConnectedAdAccountId", "A conta de anúncios conectada é obrigatória."));
        }

        if (string.IsNullOrWhiteSpace(platform))
        {
            return Result<Campaign>.Failure(
                Error.Validation("Campaign.EmptyPlatform", "A plataforma de mídia é obrigatória."));
        }

        if (string.IsNullOrWhiteSpace(externalCampaignId))
        {
            return Result<Campaign>.Failure(
                Error.Validation("Campaign.EmptyExternalCampaignId", "O identificador externo da campanha é obrigatório."));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<Campaign>.Failure(
                Error.Validation("Campaign.EmptyName", "O nome da campanha é obrigatório."));
        }

        var normalizedCurrency = string.IsNullOrWhiteSpace(currency) ? "BRL" : currency.Trim().ToUpperInvariant();
        var now = DateTime.UtcNow;
        var syncTime = lastSyncedAtUtc ?? now;

        var campaign = new Campaign(
            id,
            workspaceId,
            connectedAdAccountId,
            platform.Trim(),
            externalCampaignId.Trim(),
            name.Trim(),
            status,
            objective?.Trim() ?? string.Empty,
            dailyBudget,
            lifetimeBudget,
            normalizedCurrency,
            startDateUtc,
            endDateUtc,
            syncTime,
            now,
            null);

        return Result<Campaign>.Success(campaign);
    }

    /// <summary>
    /// Atualiza os detalhes da campanha a partir de uma nova sincronização com a rede externa.
    /// </summary>
    /// <param name="name">Novo nome da campanha.</param>
    /// <param name="status">Novo status unificado.</param>
    /// <param name="objective">Novo objetivo.</param>
    /// <param name="dailyBudget">Orçamento diário atualizado.</param>
    /// <param name="lifetimeBudget">Orçamento total atualizado.</param>
    /// <param name="currency">Moeda.</param>
    /// <param name="startDateUtc">Data de início.</param>
    /// <param name="endDateUtc">Data de término.</param>
    /// <param name="lastSyncedAtUtc">Carimbo UTC da sincronização.</param>
    /// <returns>Resultado da operação.</returns>
    public Result UpdateDetails(
        string name,
        CampaignStatus status,
        string objective,
        decimal? dailyBudget,
        decimal? lifetimeBudget,
        string currency,
        DateTime? startDateUtc,
        DateTime? endDateUtc,
        DateTime lastSyncedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(Error.Validation("Campaign.EmptyName", "O nome da campanha não pode ser vazio."));
        }

        Name = name.Trim();
        Status = status;
        Objective = objective?.Trim() ?? string.Empty;
        DailyBudget = dailyBudget;
        LifetimeBudget = lifetimeBudget;
        Currency = string.IsNullOrWhiteSpace(currency) ? Currency : currency.Trim().ToUpperInvariant();
        StartDateUtc = startDateUtc;
        EndDateUtc = endDateUtc;
        LastSyncedAtUtc = lastSyncedAtUtc;
        UpdatedAtUtc = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Ativa a veiculação da campanha no contexto de ações manuais ou em lote.
    /// </summary>
    /// <returns>Resultado da operação.</returns>
    public Result Activate()
    {
        Status = CampaignStatus.Active;
        UpdatedAtUtc = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// Pausa a veiculação da campanha no contexto de automações ou ações manuais.
    /// </summary>
    /// <returns>Resultado da operação.</returns>
    public Result Pause()
    {
        Status = CampaignStatus.Paused;
        UpdatedAtUtc = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// Atualiza o orçamento diário programado da campanha.
    /// </summary>
    /// <param name="newDailyBudget">Novo valor numérico positivo para o orçamento diário.</param>
    /// <returns>Resultado da alteração orçamentária.</returns>
    public Result UpdateDailyBudget(decimal newDailyBudget)
    {
        if (newDailyBudget < 0)
        {
            return Result.Failure(Error.Validation("Campaign.NegativeBudget", "O orçamento diário não pode ser negativo."));
        }

        DailyBudget = newDailyBudget;
        UpdatedAtUtc = DateTime.UtcNow;
        return Result.Success();
    }
}
