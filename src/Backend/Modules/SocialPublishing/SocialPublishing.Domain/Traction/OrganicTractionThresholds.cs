namespace SocialPublishing.Domain.Traction;

/// <summary>
/// Limiares nomeados para detecção de tração orgânica por workspace e rede.
/// </summary>
public static class OrganicTractionThresholds
{
    /// <summary>
    /// Quantidade mínima de posts publicados com impressões na janela para calcular a mediana.
    /// </summary>
    public const int MinimumSampleSize = 3;

    /// <summary>
    /// Janela retroativa em dias para a linha de base de engajamento.
    /// </summary>
    public const int BaselineWindowDays = 28;

    /// <summary>
    /// Impressões mínimas para um post ser candidato a hit de tração.
    /// </summary>
    public const long MinimumImpressions = 1000;

    /// <summary>
    /// Múltiplo mínimo do engajamento em relação à mediana da rede na janela.
    /// </summary>
    public const decimal EngagementMultiple = 2.0m;
}
