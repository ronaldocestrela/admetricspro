using BuildingBlocks.Domain.Automations;

namespace Automations.Domain.Services;

/// <summary>
/// Métrica consolidada em memória por escopo e janela temporal para uso na avaliação de predicados.
/// Totalmente desacoplada de conexões de banco de dados ou requisições de rede.
/// </summary>
public sealed class PerformanceMetricSnapshot
{
    /// <summary>
    /// Inicializa uma nova instância de <see cref="PerformanceMetricSnapshot"/>.
    /// </summary>
    public PerformanceMetricSnapshot(
        string platform,
        RuleScope scope,
        Guid? entityId,
        int timeWindowHours,
        decimal spend,
        long impressions,
        long clicks,
        decimal conversions,
        decimal conversionValue)
    {
        Platform = platform?.Trim() ?? "All";
        Scope = scope;
        EntityId = entityId;
        TimeWindowHours = timeWindowHours;
        Spend = spend;
        Impressions = impressions;
        Clicks = clicks;
        Conversions = conversions;
        ConversionValue = conversionValue;
    }

    /// <summary>
    /// Plataforma de publicidade (MetaAds, GoogleAds, TikTokAds, BingAds, All).
    /// </summary>
    public string Platform { get; }

    /// <summary>
    /// Escopo do dado (Workspace, Campaign, AdSet, Ad).
    /// </summary>
    public RuleScope Scope { get; }

    /// <summary>
    /// Identificador da entidade específica, se aplicável.
    /// </summary>
    public Guid? EntityId { get; }

    /// <summary>
    /// Janela temporal em horas em que as métricas foram consolidadas (ex: 24, 48, 72).
    /// </summary>
    public int TimeWindowHours { get; }

    /// <summary>
    /// Investimento monetário total no período.
    /// </summary>
    public decimal Spend { get; }

    /// <summary>
    /// Impressões totais veiculadas.
    /// </summary>
    public long Impressions { get; }

    /// <summary>
    /// Volume total de cliques.
    /// </summary>
    public long Clicks { get; }

    /// <summary>
    /// Volume de conversões.
    /// </summary>
    public decimal Conversions { get; }

    /// <summary>
    /// Valor monetário total de conversões geradas.
    /// </summary>
    public decimal ConversionValue { get; }

    /// <summary>
    /// Custo por Aquisição (CPA). Retorna 0 quando não há conversões para evitar divisão por zero.
    /// </summary>
    public decimal Cpa => Conversions > 0 ? Math.Round(Spend / Conversions, 4) : 0m;

    /// <summary>
    /// Retorno sobre Investimento em Anúncios (ROAS). Retorna 0 quando Spend for zero.
    /// </summary>
    public decimal Roas => Spend > 0 ? Math.Round(ConversionValue / Spend, 4) : 0m;

    /// <summary>
    /// Custo Médio por Clique (CPC).
    /// </summary>
    public decimal Cpc => Clicks > 0 ? Math.Round(Spend / Clicks, 4) : 0m;

    /// <summary>
    /// Custo por Mil Impressões (CPM).
    /// </summary>
    public decimal Cpm => Impressions > 0 ? Math.Round((Spend / Impressions) * 1000m, 4) : 0m;

    /// <summary>
    /// Taxa de Cliques (CTR em %).
    /// </summary>
    public decimal Ctr => Impressions > 0 ? Math.Round(((decimal)Clicks / Impressions) * 100m, 4) : 0m;

    /// <summary>
    /// Obtém o valor numérico da métrica solicitada.
    /// </summary>
    public decimal GetValueForMetric(MetricType metric) => metric switch
    {
        MetricType.Cpa => Cpa,
        MetricType.Roas => Roas,
        MetricType.Cpc => Cpc,
        MetricType.Cpm => Cpm,
        MetricType.Ctr => Ctr,
        MetricType.Spend => Spend,
        MetricType.Conversions => Conversions,
        MetricType.ConversionValue => ConversionValue,
        _ => 0m
    };
}

/// <summary>
/// Contexto em memória contendo as fotografias de performance disponibilizadas para a execução da regra.
/// </summary>
public sealed class RuleEvaluationContext
{
    private readonly List<PerformanceMetricSnapshot> _snapshots = new();

    /// <summary>
    /// Inicializa uma nova instância de <see cref="RuleEvaluationContext"/>.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="snapshots">Coleção de métricas de performance.</param>
    public RuleEvaluationContext(Guid workspaceId, IEnumerable<PerformanceMetricSnapshot>? snapshots = null)
    {
        WorkspaceId = workspaceId;
        if (snapshots != null)
        {
            _snapshots.AddRange(snapshots);
        }
    }

    /// <summary>
    /// Identificador do workspace sob avaliação.
    /// </summary>
    public Guid WorkspaceId { get; }

    /// <summary>
    /// Snapshots de métricas carregados no contexto.
    /// </summary>
    public IReadOnlyCollection<PerformanceMetricSnapshot> Snapshots => _snapshots.AsReadOnly();

    /// <summary>
    /// Adiciona um snapshot de métrica ao contexto.
    /// </summary>
    public void AddSnapshot(PerformanceMetricSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        _snapshots.Add(snapshot);
    }
}
