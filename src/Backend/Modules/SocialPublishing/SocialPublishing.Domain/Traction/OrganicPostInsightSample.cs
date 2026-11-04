using SocialPublishing.Domain.Connections;

namespace SocialPublishing.Domain.Traction;

/// <summary>
/// Amostra de métricas orgânicas de um post publicado, usada pelo detector de tração.
/// </summary>
/// <param name="WorkspaceId">Workspace dono do post.</param>
/// <param name="OrganicPostId">Identificador do post orgânico.</param>
/// <param name="Platform">Rede social do post.</param>
/// <param name="PublishedAtUtc">Instante de publicação em UTC.</param>
/// <param name="Impressions">Contagem de impressões (ou visualizações na rede).</param>
/// <param name="Likes">Curtidas ou reações equivalentes.</param>
/// <param name="Comments">Comentários.</param>
/// <param name="Shares">Compartilhamentos.</param>
/// <param name="Saves">Salvamentos (zero quando a rede não expõe a métrica).</param>
/// <param name="HasOpenSuggestion">Indica se já existe sugestão de impulsionamento aberta para o post.</param>
public sealed record OrganicPostInsightSample(
    Guid WorkspaceId,
    Guid OrganicPostId,
    OrganicPlatform Platform,
    DateTime PublishedAtUtc,
    long Impressions,
    long Likes,
    long Comments,
    long Shares,
    long Saves,
    bool HasOpenSuggestion);
