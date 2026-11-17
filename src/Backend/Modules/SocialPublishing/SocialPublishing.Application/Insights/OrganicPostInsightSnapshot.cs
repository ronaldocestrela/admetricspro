namespace SocialPublishing.Application.Insights;

/// <summary>
/// Métricas orgânicas lidas na rede para um post publicado.
/// </summary>
/// <param name="Impressions">Impressões ou visualizações conforme a rede.</param>
/// <param name="Likes">Curtidas ou reações equivalentes.</param>
/// <param name="Comments">Comentários.</param>
/// <param name="Shares">Compartilhamentos.</param>
/// <param name="Saves">Salvamentos (zero quando a rede não expõe a métrica).</param>
public sealed record OrganicPostInsightSnapshot(
    long Impressions,
    long Likes,
    long Comments,
    long Shares,
    long Saves);
