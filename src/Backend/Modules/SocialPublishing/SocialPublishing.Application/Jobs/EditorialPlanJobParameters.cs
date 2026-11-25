namespace SocialPublishing.Application.Jobs;

/// <summary>
/// Parâmetros mínimos serializados de um job de plano editorial.
/// </summary>
/// <param name="WorkspaceId">Workspace.</param>
/// <param name="RangeStart">Início inclusivo.</param>
/// <param name="RangeEnd">Fim inclusivo.</param>
/// <param name="TimeZoneId">Fuso IANA.</param>
/// <param name="PostsPerWeek">Posts por semana.</param>
/// <param name="Seed">Semente determinística.</param>
public sealed record EditorialPlanJobParameters(
    Guid WorkspaceId,
    DateOnly RangeStart,
    DateOnly RangeEnd,
    string TimeZoneId,
    int PostsPerWeek,
    int Seed);