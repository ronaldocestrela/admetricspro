using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Integrations.DTOs;
using Master.Application.Integrations.Repositories;
using Master.Application.Integrations.Services;
using Master.Domain.Integrations;

namespace Master.Application.Integrations.Queries.GetApiHealthOverview;

/// <summary>
/// Handler for <see cref="GetApiHealthOverviewQuery"/>.
/// Consolidates rate limit quotas across platforms with aggregate tenant connection status.
/// </summary>
public sealed class GetApiHealthOverviewQueryHandler : IQueryHandler<GetApiHealthOverviewQuery, ApiHealthOverviewDto>
{
    private readonly IApiQuotaTrackerService _quotaTrackerService;
    private readonly ITenantApiConnectionRepository _connectionRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetApiHealthOverviewQueryHandler"/> class.
    /// </summary>
    /// <param name="quotaTrackerService">Quota tracker service.</param>
    /// <param name="connectionRepository">Tenant API connection repository.</param>
    public GetApiHealthOverviewQueryHandler(
        IApiQuotaTrackerService quotaTrackerService,
        ITenantApiConnectionRepository connectionRepository)
    {
        _quotaTrackerService = quotaTrackerService ?? throw new ArgumentNullException(nameof(quotaTrackerService));
        _connectionRepository = connectionRepository ?? throw new ArgumentNullException(nameof(connectionRepository));
    }

    /// <inheritdoc />
    public async Task<Result<ApiHealthOverviewDto>> Handle(
        GetApiHealthOverviewQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var quotas = await _quotaTrackerService.GetAllQuotaStatusesAsync(cancellationToken);

        var totalConnections = await _connectionRepository.GetTotalCountAsync(cancellationToken);
        var connected = await _connectionRepository.CountByStatusAsync(ApiConnectionStatus.Connected, cancellationToken);
        var expiringSoon = await _connectionRepository.CountByStatusAsync(ApiConnectionStatus.ExpiringSoon, cancellationToken);
        var expired = await _connectionRepository.CountByStatusAsync(ApiConnectionStatus.Expired, cancellationToken);
        var revoked = await _connectionRepository.CountByStatusAsync(ApiConnectionStatus.Revoked, cancellationToken);
        var disconnected = await _connectionRepository.CountByStatusAsync(ApiConnectionStatus.Disconnected, cancellationToken);

        var overview = new ApiHealthOverviewDto(
            PlatformQuotas: quotas,
            TotalConnections: totalConnections,
            ConnectedCount: connected,
            ExpiringSoonCount: expiringSoon,
            ExpiredCount: expired,
            RevokedOrDisconnectedCount: revoked + disconnected,
            TimestampUtc: DateTime.UtcNow);

        return Result<ApiHealthOverviewDto>.Success(overview);
    }
}
