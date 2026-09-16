using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Domain.Campaigns;

/// <summary>
/// Entidade de domínio que representa uma métrica analítica de desempenho de campanha, conjunto ou anúncio,
/// consolidada por período (diário ou horário) e garantindo idempotência estrita em re-execuções de sincronização.
/// </summary>
public sealed class CampaignMetric : Entity<Guid>
{
    private CampaignMetric(
        Guid id,
        Guid workspaceId,
        Guid connectedAdAccountId,
        Guid campaignId,
        Guid? adSetId,
        Guid? adId,
        string platform,
        string externalCampaignId,
        string? externalAdSetId,
        string? externalAdId,
        DateTime date,
        int? hour,
        MetricGranularity granularity,
        decimal spend,
        string currency,
        long impressions,
        long clicks,
        decimal conversions,
        decimal conversionValue,
        DateTime syncedAtUtc,
        DateTime createdAtUtc,
        DateTime? updatedAtUtc)
        : base(id)
    {
        WorkspaceId = workspaceId;
        ConnectedAdAccountId = connectedAdAccountId;
        CampaignId = campaignId;
        AdSetId = adSetId;
        AdId = adId;
        Platform = platform;
        ExternalCampaignId = externalCampaignId;
        ExternalAdSetId = externalAdSetId;
        ExternalAdId = externalAdId;
        Date = date;
        Hour = hour;
        Granularity = granularity;
        Spend = spend;
        Currency = currency;
        Impressions = impressions;
        Clicks = clicks;
        Conversions = conversions;
        ConversionValue = conversionValue;
        SyncedAtUtc = syncedAtUtc;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    private CampaignMetric()
        : base(Guid.Empty)
    {
        Platform = string.Empty;
        ExternalCampaignId = string.Empty;
        Currency = "BRL";
        Granularity = MetricGranularity.Daily;
    }

    /// <summary>
    /// Identificador do workspace do inquilino.
    /// </summary>
    public Guid WorkspaceId { get; private set; }

    /// <summary>
    /// Identificador da conta conectada da rede de anúncios.
    /// </summary>
    public Guid ConnectedAdAccountId { get; private set; }

    /// <summary>
    /// Identificador da campanha associada.
    /// </summary>
    public Guid CampaignId { get; private set; }

    /// <summary>
    /// Identificador opcional do conjunto de anúncios.
    /// </summary>
    public Guid? AdSetId { get; private set; }

    /// <summary>
    /// Identificador opcional do anúncio / criativo.
    /// </summary>
    public Guid? AdId { get; private set; }

    /// <summary>
    /// Plataforma de anúncios de origem (MetaAds, GoogleAds, TikTokAds, BingAds).
    /// </summary>
    public string Platform { get; private set; }

    /// <summary>
    /// Identificador externo da campanha na rede de anúncios.
    /// </summary>
    public string ExternalCampaignId { get; private set; }

    /// <summary>
    /// Identificador externo opcional do conjunto na rede.
    /// </summary>
    public string? ExternalAdSetId { get; private set; }

    /// <summary>
    /// Identificador externo opcional do anúncio na rede.
    /// </summary>
    public string? ExternalAdId { get; private set; }

    /// <summary>
    /// Data da métrica normalizada em UTC (meia-noite do dia correspondente).
    /// </summary>
    public DateTime Date { get; private set; }

    /// <summary>
    /// Hora do dia (0 a 23) para métricas com granularidade horária. Nulo para métricas diárias.
    /// </summary>
    public int? Hour { get; private set; }

    /// <summary>
    /// Granularidade temporal da consolidação (Daily ou Hourly).
    /// </summary>
    public MetricGranularity Granularity { get; private set; }

    /// <summary>
    /// Investimento monetário total incorrido no período.
    /// </summary>
    public decimal Spend { get; private set; }

    /// <summary>
    /// Código ISO da moeda do valor investido (ex: BRL, USD).
    /// </summary>
    public string Currency { get; private set; }

    /// <summary>
    /// Número total de impressões veiculadas.
    /// </summary>
    public long Impressions { get; private set; }

    /// <summary>
    /// Número total de cliques computados.
    /// </summary>
    public long Clicks { get; private set; }

    /// <summary>
    /// Total de conversões registradas (suporta valores decimais para modelos de atribuição data-driven).
    /// </summary>
    public decimal Conversions { get; private set; }

    /// <summary>
    /// Valor financeiro total de conversão ou receita direta gerada.
    /// </summary>
    public decimal ConversionValue { get; private set; }

    /// <summary>
    /// Data e hora UTC em que o registro foi sincronizado pela última vez com a rede.
    /// </summary>
    public DateTime SyncedAtUtc { get; private set; }

    /// <summary>
    /// Data e hora UTC de criação do registro no banco local.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Data e hora UTC da última atualização do registro.
    /// </summary>
    public DateTime? UpdatedAtUtc { get; private set; }

    #region Computed KPIs

    /// <summary>
    /// Taxa de cliques (Click-Through Rate) expressa em porcentagem (ex: 5.0 para 5%).
    /// </summary>
    public decimal Ctr => Impressions > 0
        ? Math.Round(((decimal)Clicks / Impressions) * 100m, 4)
        : 0m;

    /// <summary>
    /// Custo Médio por Clique (Cost Per Click).
    /// </summary>
    public decimal Cpc => Clicks > 0
        ? Math.Round(Spend / Clicks, 4)
        : 0m;

    /// <summary>
    /// Custo por Mil Impressões (Cost Per Mille).
    /// </summary>
    public decimal Cpm => Impressions > 0
        ? Math.Round((Spend / Impressions) * 1000m, 4)
        : 0m;

    /// <summary>
    /// Custo por Aquisição / Conversão (Cost Per Action).
    /// </summary>
    public decimal Cpa => Conversions > 0
        ? Math.Round(Spend / Conversions, 4)
        : 0m;

    /// <summary>
    /// Retorno sobre Investimento Publicitário (Return On Ad Spend).
    /// </summary>
    public decimal Roas => Spend > 0
        ? Math.Round(ConversionValue / Spend, 4)
        : 0m;

    #endregion

    /// <summary>
    /// Fábrica para criação validada de uma nova métrica analítica de campanha.
    /// </summary>
    /// <param name="id">Identificador único.</param>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="connectedAdAccountId">Identificador da conta conectada.</param>
    /// <param name="campaignId">Identificador da campanha.</param>
    /// <param name="adSetId">Identificador opcional do conjunto.</param>
    /// <param name="adId">Identificador opcional do anúncio.</param>
    /// <param name="platform">Plataforma de anúncios.</param>
    /// <param name="externalCampaignId">Identificador externo da campanha.</param>
    /// <param name="externalAdSetId">Identificador externo do conjunto.</param>
    /// <param name="externalAdId">Identificador externo do anúncio.</param>
    /// <param name="date">Data da métrica.</param>
    /// <param name="hour">Hora do dia (obrigatória para granularidade horária).</param>
    /// <param name="granularity">Granularidade temporal.</param>
    /// <param name="spend">Investimento monetário.</param>
    /// <param name="currency">Moeda.</param>
    /// <param name="impressions">Número de impressões.</param>
    /// <param name="clicks">Número de cliques.</param>
    /// <param name="conversions">Número de conversões.</param>
    /// <param name="conversionValue">Valor financeiro de conversão.</param>
    /// <param name="syncedAtUtc">Data/hora UTC da sincronização (opcional).</param>
    /// <returns>Resultado contendo a instância de <see cref="CampaignMetric"/> ou erro de validação.</returns>
    public static Result<CampaignMetric> Create(
        Guid id,
        Guid workspaceId,
        Guid connectedAdAccountId,
        Guid campaignId,
        Guid? adSetId,
        Guid? adId,
        string platform,
        string externalCampaignId,
        string? externalAdSetId,
        string? externalAdId,
        DateTime date,
        int? hour,
        MetricGranularity granularity,
        decimal spend,
        string currency,
        long impressions,
        long clicks,
        decimal conversions,
        decimal conversionValue,
        DateTime? syncedAtUtc = null)
    {
        if (id == Guid.Empty)
            return Result<CampaignMetric>.Failure(Error.Validation("CampaignMetric.InvalidId", "O ID da métrica é obrigatório."));

        if (workspaceId == Guid.Empty)
            return Result<CampaignMetric>.Failure(Error.Validation("CampaignMetric.InvalidWorkspaceId", "O WorkspaceId é obrigatório."));

        if (connectedAdAccountId == Guid.Empty)
            return Result<CampaignMetric>.Failure(Error.Validation("CampaignMetric.InvalidAccountId", "O ConnectedAdAccountId é obrigatório."));

        if (campaignId == Guid.Empty)
            return Result<CampaignMetric>.Failure(Error.Validation("CampaignMetric.InvalidCampaignId", "O CampaignId é obrigatório."));

        if (string.IsNullOrWhiteSpace(platform))
            return Result<CampaignMetric>.Failure(Error.Validation("CampaignMetric.InvalidPlatform", "A plataforma de anúncios é obrigatória."));

        if (string.IsNullOrWhiteSpace(externalCampaignId))
            return Result<CampaignMetric>.Failure(Error.Validation("CampaignMetric.InvalidExternalCampaignId", "O ID externo da campanha é obrigatório."));

        if (granularity == MetricGranularity.Hourly)
        {
            if (!hour.HasValue || hour.Value < 0 || hour.Value > 23)
            {
                return Result<CampaignMetric>.Failure(Error.Validation(
                    "CampaignMetric.InvalidHour",
                    "Para métricas horárias, a hora deve ser especificada entre 0 e 23."));
            }
        }
        else
        {
            hour = null;
        }

        if (spend < 0 || impressions < 0 || clicks < 0 || conversions < 0 || conversionValue < 0)
        {
            return Result<CampaignMetric>.Failure(Error.Validation(
                "CampaignMetric.InvalidValues",
                "Métricas de gasto, impressões, cliques, conversões e receita não podem ser negativas."));
        }

        var normalizedDate = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        var syncTime = syncedAtUtc ?? DateTime.UtcNow;
        var currencyCode = string.IsNullOrWhiteSpace(currency) ? "BRL" : currency.Trim().ToUpperInvariant();

        var metric = new CampaignMetric(
            id,
            workspaceId,
            connectedAdAccountId,
            campaignId,
            adSetId,
            adId,
            platform.Trim(),
            externalCampaignId.Trim(),
            externalAdSetId?.Trim(),
            externalAdId?.Trim(),
            normalizedDate,
            hour,
            granularity,
            spend,
            currencyCode,
            impressions,
            clicks,
            conversions,
            conversionValue,
            syncTime,
            createdAtUtc: syncTime,
            updatedAtUtc: null);

        return Result<CampaignMetric>.Success(metric);
    }

    /// <summary>
    /// Atualiza de forma idempotente os valores de métricas em re-execuções de ingestão.
    /// </summary>
    /// <param name="newSpend">Novo gasto consolidado.</param>
    /// <param name="newImpressions">Novas impressões consolidadas.</param>
    /// <param name="newClicks">Novos cliques consolidados.</param>
    /// <param name="newConversions">Novas conversões consolidadas.</param>
    /// <param name="newConversionValue">Novo valor de conversão consolidado.</param>
    /// <param name="syncTimeUtc">Data e hora UTC da sincronização.</param>
    /// <returns>Resultado da operação.</returns>
    public Result UpdateMetrics(
        decimal newSpend,
        long newImpressions,
        long newClicks,
        decimal newConversions,
        decimal newConversionValue,
        DateTime syncTimeUtc)
    {
        if (newSpend < 0 || newImpressions < 0 || newClicks < 0 || newConversions < 0 || newConversionValue < 0)
        {
            return Result.Failure(Error.Validation(
                "CampaignMetric.InvalidValues",
                "Métricas atualizadas não podem ser negativas."));
        }

        Spend = newSpend;
        Impressions = newImpressions;
        Clicks = newClicks;
        Conversions = newConversions;
        ConversionValue = newConversionValue;
        SyncedAtUtc = syncTimeUtc;
        UpdatedAtUtc = syncTimeUtc;

        return Result.Success();
    }
}
