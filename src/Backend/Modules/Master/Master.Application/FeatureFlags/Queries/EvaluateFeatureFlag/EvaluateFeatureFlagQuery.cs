using BuildingBlocks.Application.Messaging;

namespace Master.Application.FeatureFlags.Queries.EvaluateFeatureFlag;

/// <summary>
/// Query to evaluate whether a feature flag is enabled for an optional tenant context.
/// </summary>
/// <param name="Key">Unique feature flag key.</param>
/// <param name="TenantId">Optional tenant identifier.</param>
public sealed record EvaluateFeatureFlagQuery(string Key, Guid? TenantId = null) : IQuery<bool>;
