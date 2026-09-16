namespace WebApi.Models;

/// <summary>
/// Modelo de requisição para execução em 1 clique de uma recomendação do Copiloto de IA via API.
/// </summary>
public sealed class ExecuteCopilotActionApiRequest
{
    /// <summary>
    /// Inicializa uma nova instância padrão de <see cref="ExecuteCopilotActionApiRequest"/>.
    /// </summary>
    public ExecuteCopilotActionApiRequest()
    {
    }

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ExecuteCopilotActionApiRequest"/> com os parâmetros informados.
    /// </summary>
    /// <param name="workspaceId">Identificador único do workspace associado.</param>
    /// <param name="actionId">Identificador único da ação recomendada.</param>
    /// <param name="actionType">Tipo da ação a ser executada.</param>
    /// <param name="targetEntityId">Identificador da entidade alvo.</param>
    /// <param name="targetEntityName">Nome descritivo da entidade alvo.</param>
    /// <param name="platform">Plataforma de mídia.</param>
    /// <param name="title">Título da ação recomendada.</param>
    /// <param name="description">Descrição detalhada do efeito da ação.</param>
    /// <param name="parameters">Parâmetros adicionais e dinâmicos para execução.</param>
    public ExecuteCopilotActionApiRequest(
        Guid workspaceId,
        Guid actionId,
        string actionType,
        Guid targetEntityId,
        string targetEntityName,
        string platform,
        string title,
        string description,
        IReadOnlyDictionary<string, string>? parameters = null)
    {
        WorkspaceId = workspaceId;
        ActionId = actionId;
        ActionType = actionType;
        TargetEntityId = targetEntityId;
        TargetEntityName = targetEntityName;
        Platform = platform;
        Title = title;
        Description = description;
        Parameters = parameters;
    }

    /// <summary>
    /// Identificador único do workspace associado.
    /// </summary>
    public Guid WorkspaceId { get; init; }

    /// <summary>
    /// Identificador único da ação recomendada.
    /// </summary>
    public Guid ActionId { get; init; }

    /// <summary>
    /// Tipo da ação a ser executada (ex: PauseAdSet, ExcludeAudience, AddNegativeKeyword).
    /// </summary>
    public string ActionType { get; init; } = string.Empty;

    /// <summary>
    /// Identificador da entidade alvo afetada pela ação.
    /// </summary>
    public Guid TargetEntityId { get; init; }

    /// <summary>
    /// Nome descritivo da entidade alvo.
    /// </summary>
    public string TargetEntityName { get; init; } = string.Empty;

    /// <summary>
    /// Plataforma de mídia da entidade alvo (MetaAds, GoogleAds, BingAds, TikTokAds).
    /// </summary>
    public string Platform { get; init; } = string.Empty;

    /// <summary>
    /// Título da ação recomendada para exibição em interfaces e relatórios.
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Descrição detalhada do efeito da ação e justificativa do Copiloto.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Parâmetros adicionais e dinâmicos para a execução da ação.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Parameters { get; init; }
}
