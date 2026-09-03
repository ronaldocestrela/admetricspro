using BuildingBlocks.Application.Messaging;
using Master.Application.FeatureFlags.DTOs;

namespace Master.Application.FeatureFlags.Queries.GetFeatureFlagByKey;

/// <summary>
/// Query to find a single feature flag by its key.
/// </summary>
/// <param name="Key">Unique key of the flag.</param>
public sealed record GetFeatureFlagByKeyQuery(string Key) : IQuery<FeatureFlagDto>;
