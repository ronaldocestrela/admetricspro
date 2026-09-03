using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Integrations.DTOs;
using Master.Application.Integrations.Repositories;
using Master.Domain.Integrations;

namespace Master.Application.Integrations.Queries.GetTenantApiConnections;

/// <summary>
/// Handler for <see cref="GetTenantApiConnectionsQuery"/>.
/// Queries tenant API connections and maps to DTOs.
/// </summary>
public sealed class GetTenantApiConnectionsQueryHandler : IQueryHandler<GetTenantApiConnectionsQuery, IReadOnlyList<TenantApiConnectionDto>>
{
    private readonly ITenantApiConnectionRepository _connectionRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetTenantApiConnectionsQueryHandler"/> class.
    /// </summary>
    /// <param name="connectionRepository">Tenant API connection repository.</param>
    public GetTenantApiConnectionsQueryHandler(ITenantApiConnectionRepository connectionRepository)
    {
        _connectionRepository = connectionRepository ?? throw new ArgumentNullException(nameof(connectionRepository));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<TenantApiConnectionDto>>> Handle(
        GetTenantApiConnectionsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var connections = await _connectionRepository.GetConnectionsAsync(
            query.Platform,
            query.Status,
            cancellationToken);

        // Apply in-memory paging if needed
        var paged = connections
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => new TenantApiConnectionDto(
                Id: c.Id,
                TenantId: c.TenantId,
                TenantName: c.TenantName,
                Platform: c.Platform,
                PlatformName: FormatPlatformName(c.Platform),
                AccountIdentifier: c.AccountIdentifier,
                AccountName: c.AccountName,
                Status: c.Status,
                TokenExpiresAtUtc: c.TokenExpiresAtUtc,
                LastSyncAtUtc: c.LastSyncAtUtc,
                ErrorMessage: c.ErrorMessage,
                UpdatedAtUtc: c.UpdatedAtUtc))
            .ToList();

        return Result<IReadOnlyList<TenantApiConnectionDto>>.Success(paged);
    }

    private static string FormatPlatformName(AdPlatform platform) => platform switch
    {
        AdPlatform.Meta => "Meta Graph API",
        AdPlatform.Google => "Google Ads API",
        AdPlatform.TikTok => "TikTok Marketing API",
        AdPlatform.Bing => "Bing Ads API",
        _ => platform.ToString()
    };
}
