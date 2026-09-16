namespace Analytics.Domain.Copilot;

/// <summary>
/// Níveis de severidade atribuídos às anomalias operacionais detectadas pelo Copiloto de IA.
/// </summary>
public enum CopilotAnomalySeverity
{
    /// <summary>
    /// Anomalia leve ou recomendação de monitoramento preventivo.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Atenção necessária para evitar degradação de métricas secundárias.
    /// </summary>
    Medium = 1,

    /// <summary>
    /// Impacto financeiro direto ou inflação de custos relevante.
    /// </summary>
    High = 2,

    /// <summary>
    /// Desperdício severo de orçamento, auto-concorrência destrutiva ou parada operacional.
    /// </summary>
    Critical = 3
}
