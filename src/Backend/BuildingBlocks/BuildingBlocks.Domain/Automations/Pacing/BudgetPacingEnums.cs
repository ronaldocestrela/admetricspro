namespace BuildingBlocks.Domain.Automations.Pacing;

/// <summary>
/// Classificação da velocidade e ritmo de consumo do orçamento em relação ao tempo decorrido do ciclo mensal.
/// </summary>
public enum PacingStatus
{
    /// <summary>
    /// O consumo está alinhado com a taxa ideal esperada dentro da margem de tolerância.
    /// </summary>
    OnTrack = 1,

    /// <summary>
    /// O consumo está acelerado acima do esperado, com risco de esgotamento prematuro antes do término do mês.
    /// </summary>
    Over = 2,

    /// <summary>
    /// O consumo está desacelerado abaixo do esperado, com risco de subinvestimento e sobra de verba contratada.
    /// </summary>
    Under = 3
}
