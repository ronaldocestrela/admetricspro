namespace Analytics.Domain.Creatives;

/// <summary>
/// Resultado da análise preditiva de fadiga e saturação de audiência de um criativo.
/// </summary>
/// <param name="AdId">Identificador único do anúncio analisado.</param>
/// <param name="AdName">Nome descritivo do anúncio.</param>
/// <param name="Platform">Plataforma em que o anúncio está sendo veiculado (ex: MetaAds, TikTokAds).</param>
/// <param name="PreviewUrl">URL de pré-visualização ou miniatura do criativo.</param>
/// <param name="Status">Status classificado de integridade ou fadiga do criativo.</param>
/// <param name="AnalyzedDays">Número de dias contínuos avaliados na janela temporal.</param>
/// <param name="InitialCtr">CTR registrado no início do período avaliado.</param>
/// <param name="CurrentCtr">CTR registrado no final do período avaliado.</param>
/// <param name="CtrChangePercentage">Variação percentual do CTR no período avaliado (ex: -32.5 para uma queda de 32.5%).</param>
/// <param name="CtrTrendSlope">Inclinação da reta de regressão linear diária de CTR (negativa indica queda contínua).</param>
/// <param name="AverageFrequency">Frequência média acumulada de exibição no período.</param>
/// <param name="CurrentFrequency">Frequência mais recente observada.</param>
/// <param name="ReplacementSuggested">Indica se a substituição da peça publicitária é expressamente sugerida.</param>
/// <param name="Reason">Justificativa técnica analítica do diagnóstico gerado.</param>
/// <param name="ActionRecommendation">Ação recomendada para o gestor de tráfego.</param>
public sealed record CreativeFatigueAnalysisResult(
    Guid AdId,
    string AdName,
    string Platform,
    string? PreviewUrl,
    CreativeFatigueStatus Status,
    int AnalyzedDays,
    decimal InitialCtr,
    decimal CurrentCtr,
    decimal CtrChangePercentage,
    decimal CtrTrendSlope,
    decimal AverageFrequency,
    decimal CurrentFrequency,
    bool ReplacementSuggested,
    string Reason,
    string ActionRecommendation);
