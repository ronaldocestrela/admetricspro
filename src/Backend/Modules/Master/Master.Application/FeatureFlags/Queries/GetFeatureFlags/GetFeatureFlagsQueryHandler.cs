using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Master.Application.FeatureFlags.DTOs;
using Master.Application.FeatureFlags.Services;

namespace Master.Application.FeatureFlags.Queries.GetFeatureFlags;

/// <summary>
/// Query handler that fetches all feature flags.
/// </summary>
public sealed class GetFeatureFlagsQueryHandler : IQueryHandler<GetFeatureFlagsQuery, IReadOnlyList<FeatureFlagDto>>
{
    private readonly IFeatureFlagService _featureFlagService;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetFeatureFlagsQueryHandler"/> class.
    /// </summary>
    /// <param name="featureFlagService">Feature flag service.</param>
    public GetFeatureFlagsQueryHandler(IFeatureFlagService featureFlagService)
    {
        _featureFlagService = featureFlagService ?? throw new ArgumentNullException(nameof(featureFlagService));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<FeatureFlagDto>>> Handle(GetFeatureFlagsQuery request, CancellationToken cancellationToken)
    {
        return await _featureFlagService.GetAllAsync(cancellationToken);
    }
}
