namespace Analytics.Domain.Copilot;

/// <summary>
/// Tipos de ação de remediação suportados pelo mecanismo de Execução em 1 Clique.
/// </summary>
public enum CopilotActionType
{
    /// <summary>
    /// Pausar o conjunto de anúncios redundante de menor eficiência.
    /// </summary>
    PauseAdSet = 1,

    /// <summary>
    /// Aplicar exclusão mútua de audiência entre conjuntos com alta sobreposição.
    /// </summary>
    ExcludeAudience = 2,

    /// <summary>
    /// Adicionar palavra-chave negativa na campanha ou canal ineficiente para estancar canibalização.
    /// </summary>
    AddNegativeKeyword = 3,

    /// <summary>
    /// Pausar palavra-chave em leilão inflacionado por auto-concorrência.
    /// </summary>
    PauseKeyword = 4,

    /// <summary>
    /// Reajustar o orçamento ou limitar teto de lance máximo de CPC/CPA.
    /// </summary>
    AdjustBudget = 5
}
