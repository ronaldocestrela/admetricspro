using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Master.Application.FeatureFlags.DTOs;
using Master.Application.FeatureFlags.Services;

namespace Master.Application.FeatureFlags.Queries.GetFeatureFlagByKey;

/// <summary>
/// Query handler that fetches a single feature flag by key.
/// </summary>
public sealed class GetFeatureFlagByKeyQueryHandler : IQueryHandler<GetFeatureFlagByKeyQuery, FeatureFlagDto>
{
    private readonly IFeatureFlagService _featureFlagService;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetFeatureFlagByKeyQueryHandler"/> class.
    /// </summary>
    /// <param name="featureFlagService">Feature flag service.</param>
    public GetFeatureFlagByKeyQueryHandler(IFeatureFlagService featureFlagService)
    {
        _featureFlagService = featureFlagService ?? throw new ArgumentNullException(nameof(featureFlagService));
    }

    /// <inheritdoc />
    public async Task<Result<FeatureFlagDto>> Handle(GetFeatureFlagByKeyQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await _featureFlagService.GetByKeyAsync(request.Key, cancellationToken);
    }
}
