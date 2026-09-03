namespace Master.Application.Integrations.DTOs;

/// <summary>
/// Data transfer object summarizing overall API rate limit quotas and tenant connection health.
/// </summary>
/// <param name="PlatformQuotas">List of quota statuses for all integrated ad platforms.</param>
/// <param name="TotalConnections">Total number of tenant integrations tracked.</param>
/// <param name="ConnectedCount">Number of active, healthy connections.</param>
/// <param name="ExpiringSoonCount">Number of tokens expiring within 7 days.</param>
/// <param name="ExpiredCount">Number of expired tokens requiring reconnection.</param>
/// <param name="RevokedOrDisconnectedCount">Number of revoked or disconnected accounts.</param>
/// <param name="TimestampUtc">UTC timestamp of the overview generation.</param>
public sealed record ApiHealthOverviewDto(
    IReadOnlyList<PlatformQuotaStatusDto> PlatformQuotas,
    int TotalConnections,
    int ConnectedCount,
    int ExpiringSoonCount,
    int ExpiredCount,
    int RevokedOrDisconnectedCount,
    DateTime TimestampUtc);
