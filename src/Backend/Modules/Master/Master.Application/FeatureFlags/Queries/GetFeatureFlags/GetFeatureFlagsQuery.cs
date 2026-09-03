using BuildingBlocks.Application.Messaging;
using Master.Application.FeatureFlags.DTOs;

namespace Master.Application.FeatureFlags.Queries.GetFeatureFlags;

/// <summary>
/// Query to retrieve all feature flags and operational kill switches from the central catalog.
/// </summary>
public sealed record GetFeatureFlagsQuery : IQuery<IReadOnlyList<FeatureFlagDto>>;
