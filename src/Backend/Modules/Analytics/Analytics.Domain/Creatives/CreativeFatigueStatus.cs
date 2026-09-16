namespace Analytics.Domain.Creatives;

/// <summary>
/// Define os níveis de saturação e integridade de desempenho de um anúncio criativo.
/// </summary>
public enum CreativeFatigueStatus
{
    /// <summary>
    /// Desempenho estável ou em ascensão, sem evidência de saturação de público.
    /// </summary>
    Healthy = 1,

    /// <summary>
    /// Sinais iniciais de desgaste (leve queda de CTR ou frequência em elevação).
    /// </summary>
    Warning = 2,

    /// <summary>
    /// Fadiga severa detectada com queda acentuada de CTR e frequência de exibição crítica.
    /// A substituição ou renovação do criativo é sugerida.
    /// </summary>
    Fatigued = 3
}
