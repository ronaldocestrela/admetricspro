using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Master.Application.FeatureFlags.Services;

namespace Master.Application.FeatureFlags.Queries.EvaluateFeatureFlag;

/// <summary>
/// Query handler that evaluates a feature flag for a given tenant context.
/// </summary>
public sealed class EvaluateFeatureFlagQueryHandler : IQueryHandler<EvaluateFeatureFlagQuery, bool>
{
    private readonly IFeatureFlagService _featureFlagService;

    /// <summary>
    /// Initializes a new instance of the <see cref="EvaluateFeatureFlagQueryHandler"/> class.
    /// </summary>
    /// <param name="featureFlagService">Feature flag service.</param>
    public EvaluateFeatureFlagQueryHandler(IFeatureFlagService featureFlagService)
    {
        _featureFlagService = featureFlagService ?? throw new ArgumentNullException(nameof(featureFlagService));
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(EvaluateFeatureFlagQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await _featureFlagService.EvaluateAsync(request.Key, request.TenantId, cancellationToken);
    }
}
