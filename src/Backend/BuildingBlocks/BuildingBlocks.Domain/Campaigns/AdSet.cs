using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Domain.Campaigns;

/// <summary>
/// Entidade de domínio universal que representa um conjunto ou grupo de anúncios (AdSet / AdGroup).
/// Subordinado à Campanha e agrupador de criativos/anúncios.
/// </summary>
public sealed class AdSet : Entity<Guid>
{
    private readonly List<Ad> _ads = new();

    private AdSet(
        Guid id,
        Guid campaignId,
        Guid connectedAdAccountId,
        string externalAdSetId,
        string name,
        AdSetStatus status,
        string? bidStrategy,
        string? optimizationGoal,
        decimal? dailyBudget,
        decimal? lifetimeBudget,
        string? targetingSummary,
        DateTime? startDateUtc,
        DateTime? endDateUtc,
        DateTime lastSyncedAtUtc,
        DateTime createdAtUtc,
        DateTime? updatedAtUtc)
        : base(id)
    {
        CampaignId = campaignId;
        ConnectedAdAccountId = connectedAdAccountId;
        ExternalAdSetId = externalAdSetId;
        Name = name;
        Status = status;
        BidStrategy = bidStrategy;
        OptimizationGoal = optimizationGoal;
        DailyBudget = dailyBudget;
        LifetimeBudget = lifetimeBudget;
        TargetingSummary = targetingSummary;
        StartDateUtc = startDateUtc;
        EndDateUtc = endDateUtc;
        LastSyncedAtUtc = lastSyncedAtUtc;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    private AdSet()
        : base(Guid.Empty)
    {
        ExternalAdSetId = string.Empty;
        Name = string.Empty;
        Status = AdSetStatus.Unknown;
    }

    /// <summary>
    /// Obtém o identificador único da campanha pai à qual o conjunto pertence.
    /// </summary>
    public Guid CampaignId { get; private set; }

    /// <summary>
    /// Obtém o identificador da conta de anúncios conectada associada.
    /// </summary>
    public Guid ConnectedAdAccountId { get; private set; }

    /// <summary>
    /// Obtém o identificador nativo do conjunto na rede externa (ex: adset_123456 ou adgroup_123456).
    /// </summary>
    public string ExternalAdSetId { get; private set; }

    /// <summary>
    /// Obtém o nome amigável do conjunto de anúncios.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Obtém o status operacional unificado do conjunto.
    /// </summary>
    public AdSetStatus Status { get; private set; }

    /// <summary>
    /// Obtém a estratégia de lances adotada (ex: LOWEST_COST, TARGET_CPA, MAXIMIZE_CONVERSIONS).
    /// </summary>
    public string? BidStrategy { get; private set; }

    /// <summary>
    /// Obtém a meta de otimização (ex: OFFSITE_CONVERSIONS, LANDING_PAGE_VIEWS, IMPRESSIONS).
    /// </summary>
    public string? OptimizationGoal { get; private set; }

    /// <summary>
    /// Obtém o orçamento diário alocado ao nível de conjunto, quando não configurado em nível de campanha.
    /// </summary>
    public decimal? DailyBudget { get; private set; }

    /// <summary>
    /// Obtém o orçamento total alocado ao conjunto.
    /// </summary>
    public decimal? LifetimeBudget { get; private set; }

    /// <summary>
    /// Obtém um resumo estruturado (JSON ou texto) da segmentação de público (idade, gênero, localização, interesses).
    /// </summary>
    public string? TargetingSummary { get; private set; }

    /// <summary>
    /// Obtém a data UTC de início da veiculação do conjunto.
    /// </summary>
    public DateTime? StartDateUtc { get; private set; }

    /// <summary>
    /// Obtém a data UTC de término programado da veiculação.
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
    /// Obtém a coleção de anúncios vinculados a este conjunto.
    /// </summary>
    public IReadOnlyCollection<Ad> Ads => _ads.AsReadOnly();

    /// <summary>
    /// Cria uma nova instância de <see cref="AdSet"/> validando as invariantes de negócio.
    /// </summary>
    /// <param name="id">Identificador único do conjunto.</param>
    /// <param name="campaignId">Identificador da campanha pai.</param>
    /// <param name="connectedAdAccountId">Identificador da conta conectada.</param>
    /// <param name="externalAdSetId">Identificador externo na plataforma de origem.</param>
    /// <param name="name">Nome do conjunto.</param>
    /// <param name="status">Status operacional unificado.</param>
    /// <param name="bidStrategy">Estratégia de lances opcional.</param>
    /// <param name="optimizationGoal">Objetivo de otimização opcional.</param>
    /// <param name="dailyBudget">Orçamento diário opcional.</param>
    /// <param name="lifetimeBudget">Orçamento total opcional.</param>
    /// <param name="targetingSummary">Resumo de segmentação opcional.</param>
    /// <param name="startDateUtc">Data de início opcional.</param>
    /// <param name="endDateUtc">Data de término opcional.</param>
    /// <param name="lastSyncedAtUtc">Carimbo UTC da sincronização.</param>
    /// <returns>Resultado contendo a entidade ou erro de validação.</returns>
    public static Result<AdSet> Create(
        Guid id,
        Guid campaignId,
        Guid connectedAdAccountId,
        string externalAdSetId,
        string name,
        AdSetStatus status,
        string? bidStrategy = null,
        string? optimizationGoal = null,
        decimal? dailyBudget = null,
        decimal? lifetimeBudget = null,
        string? targetingSummary = null,
        DateTime? startDateUtc = null,
        DateTime? endDateUtc = null,
        DateTime? lastSyncedAtUtc = null)
    {
        if (id == Guid.Empty)
        {
            return Result<AdSet>.Failure(
                Error.Validation("AdSet.EmptyId", "O identificador do conjunto não pode ser vazio."));
        }

        if (campaignId == Guid.Empty)
        {
            return Result<AdSet>.Failure(
                Error.Validation("AdSet.EmptyCampaignId", "A campanha pai associada é obrigatória."));
        }

        if (connectedAdAccountId == Guid.Empty)
        {
            return Result<AdSet>.Failure(
                Error.Validation("AdSet.EmptyConnectedAdAccountId", "A conta de anúncios conectada é obrigatória."));
        }

        if (string.IsNullOrWhiteSpace(externalAdSetId))
        {
            return Result<AdSet>.Failure(
                Error.Validation("AdSet.EmptyExternalAdSetId", "O identificador externo do conjunto é obrigatório."));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<AdSet>.Failure(
                Error.Validation("AdSet.EmptyName", "O nome do conjunto não pode ser vazio."));
        }

        var now = DateTime.UtcNow;
        var syncTime = lastSyncedAtUtc ?? now;

        var adSet = new AdSet(
            id,
            campaignId,
            connectedAdAccountId,
            externalAdSetId.Trim(),
            name.Trim(),
            status,
            bidStrategy?.Trim(),
            optimizationGoal?.Trim(),
            dailyBudget,
            lifetimeBudget,
            targetingSummary?.Trim(),
            startDateUtc,
            endDateUtc,
            syncTime,
            now,
            null);

        return Result<AdSet>.Success(adSet);
    }

    /// <summary>
    /// Atualiza os detalhes operacionais do conjunto a partir de sincronização.
    /// </summary>
    /// <param name="name">Novo nome.</param>
    /// <param name="status">Novo status.</param>
    /// <param name="bidStrategy">Estratégia de lance.</param>
    /// <param name="optimizationGoal">Objetivo de otimização.</param>
    /// <param name="dailyBudget">Orçamento diário.</param>
    /// <param name="lifetimeBudget">Orçamento total.</param>
    /// <param name="targetingSummary">Segmentação.</param>
    /// <param name="startDateUtc">Data de início.</param>
    /// <param name="endDateUtc">Data de término.</param>
    /// <param name="lastSyncedAtUtc">Carimbo UTC da sincronização.</param>
    /// <returns>Resultado da operação.</returns>
    public Result UpdateDetails(
        string name,
        AdSetStatus status,
        string? bidStrategy,
        string? optimizationGoal,
        decimal? dailyBudget,
        decimal? lifetimeBudget,
        string? targetingSummary,
        DateTime? startDateUtc,
        DateTime? endDateUtc,
        DateTime lastSyncedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(Error.Validation("AdSet.EmptyName", "O nome do conjunto não pode ser vazio."));
        }

        Name = name.Trim();
        Status = status;
        BidStrategy = bidStrategy?.Trim();
        OptimizationGoal = optimizationGoal?.Trim();
        DailyBudget = dailyBudget;
        LifetimeBudget = lifetimeBudget;
        TargetingSummary = targetingSummary?.Trim();
        StartDateUtc = startDateUtc;
        EndDateUtc = endDateUtc;
        LastSyncedAtUtc = lastSyncedAtUtc;
        UpdatedAtUtc = DateTime.UtcNow;

        return Result.Success();
    }
}
