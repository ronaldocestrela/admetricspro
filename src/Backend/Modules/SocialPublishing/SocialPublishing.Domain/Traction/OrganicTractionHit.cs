using SocialPublishing.Domain.Connections;

namespace SocialPublishing.Domain.Traction;

/// <summary>
/// Post orgânico identificado como anomalia positiva de engajamento em relação à mediana do workspace na rede.
/// </summary>
/// <param name="OrganicPostId">Identificador do post.</param>
/// <param name="Platform">Rede social.</param>
/// <param name="Impressions">Impressões no momento da detecção.</param>
/// <param name="EngagementRate">Taxa (curtidas + comentários + compartilhamentos + salvamentos) / impressões.</param>
/// <param name="BaselineMedianEngagement">Mediana de engajamento dos posts na janela, mesma rede.</param>
/// <param name="EngagementMultiple">Quociente entre a taxa do post e a mediana.</param>
public sealed record OrganicTractionHit(
    Guid OrganicPostId,
    OrganicPlatform Platform,
    long Impressions,
    decimal EngagementRate,
    decimal BaselineMedianEngagement,
    decimal EngagementMultiple);
