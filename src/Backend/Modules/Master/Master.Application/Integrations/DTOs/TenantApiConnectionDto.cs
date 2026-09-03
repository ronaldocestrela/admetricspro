using Master.Domain.Integrations;

namespace Master.Application.Integrations.DTOs;

/// <summary>
/// Data transfer object representing the health of an ad platform connection for a tenant.
/// </summary>
/// <param name="Id">Connection identifier.</param>
/// <param name="TenantId">Tenant identifier.</param>
/// <param name="TenantName">Tenant organization name.</param>
/// <param name="Platform">Ad platform.</param>
/// <param name="PlatformName">Display platform name.</param>
/// <param name="AccountIdentifier">Ad account external identifier.</param>
/// <param name="AccountName">Friendly account name.</param>
/// <param name="Status">Health and validity status of the credential token.</param>
/// <param name="TokenExpiresAtUtc">Token expiration date, if known.</param>
/// <param name="LastSyncAtUtc">Last successful synchronization timestamp.</param>
/// <param name="ErrorMessage">Error or revocation details, if any.</param>
/// <param name="UpdatedAtUtc">Timestamp of the last status update.</param>
public sealed record TenantApiConnectionDto(
    Guid Id,
    Guid TenantId,
    string TenantName,
    AdPlatform Platform,
    string PlatformName,
    string AccountIdentifier,
    string AccountName,
    ApiConnectionStatus Status,
    DateTime? TokenExpiresAtUtc,
    DateTime? LastSyncAtUtc,
    string? ErrorMessage,
    DateTime UpdatedAtUtc);
