namespace Analytics.Domain.Copilot;

/// <summary>
/// Modelo que representa uma ação de otimização recomendada pronta para execução em 1 clique.
/// </summary>
public sealed record CopilotRecommendationAction
{
    /// <summary>
    /// Identificador único da ação de remediação.
    /// </summary>
    public Guid ActionId { get; init; }

    /// <summary>
    /// Tipo de ação a ser despachada.
    /// </summary>
    public CopilotActionType ActionType { get; init; }

    /// <summary>
    /// Identificador da entidade alvo (AdSetId, CampaignId, etc.).
    /// </summary>
    public Guid TargetEntityId { get; init; }

    /// <summary>
    /// Nome descritivo da entidade alvo.
    /// </summary>
    public string TargetEntityName { get; init; } = string.Empty;

    /// <summary>
    /// Plataforma de mídia de destino (MetaAds, GoogleAds, BingAds, TikTokAds).
    /// </summary>
    public string Platform { get; init; } = string.Empty;

    /// <summary>
    /// Título resumido da ação para exibição no botão e no card.
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Descrição detalhada do efeito da execução.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Parâmetros operacionais específicos da ação (ex.: termo a negativar, percentual de corte).
    /// </summary>
    public IReadOnlyDictionary<string, string> Parameters { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Indica se a ação já foi executada pelo gestor.
    /// </summary>
    public bool IsApplied { get; init; }

    /// <summary>
    /// Data e hora UTC em que a ação foi executada, se aplicável.
    /// </summary>
    public DateTime? AppliedAtUtc { get; init; }

    /// <summary>
    /// Cria uma nova instância de <see cref="CopilotRecommendationAction"/>.
    /// </summary>
    public CopilotRecommendationAction(
        Guid actionId,
        CopilotActionType actionType,
        Guid targetEntityId,
        string targetEntityName,
        string platform,
        string title,
        string description,
        IReadOnlyDictionary<string, string>? parameters = null,
        bool isApplied = false,
        DateTime? appliedAtUtc = null)
    {
        ActionId = actionId == Guid.Empty ? Guid.NewGuid() : actionId;
        ActionType = actionType;
        TargetEntityId = targetEntityId;
        TargetEntityName = targetEntityName;
        Platform = platform;
        Title = title;
        Description = description;
        Parameters = parameters ?? new Dictionary<string, string>();
        IsApplied = isApplied;
        AppliedAtUtc = appliedAtUtc;
    }
}
