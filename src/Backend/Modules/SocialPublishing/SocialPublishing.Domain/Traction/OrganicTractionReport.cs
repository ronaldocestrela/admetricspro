namespace SocialPublishing.Domain.Traction;

/// <summary>
/// Resultado da detecção de tração orgânica para um workspace em um instante de referência.
/// </summary>
/// <param name="Hits">Posts elegíveis identificados como tração.</param>
/// <param name="SkippedDueToOpenSuggestion">Posts que passaram no limiar, mas já possuem sugestão aberta.</param>
public sealed record OrganicTractionReport(
    IReadOnlyList<OrganicTractionHit> Hits,
    IReadOnlyList<OrganicTractionSkippedPost> SkippedDueToOpenSuggestion);
