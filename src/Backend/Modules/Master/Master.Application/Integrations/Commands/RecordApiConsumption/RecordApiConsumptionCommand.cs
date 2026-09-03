using BuildingBlocks.Application.Messaging;
using Master.Application.Integrations.DTOs;
using Master.Domain.Integrations;

namespace Master.Application.Integrations.Commands.RecordApiConsumption;

/// <summary>
/// Command to register consumed API operations against an ad platform rate limit quota.
/// </summary>
/// <param name="Platform">Target ad platform.</param>
/// <param name="Units">Number of calls or operations consumed.</param>
/// <param name="TimestampUtc">Optional UTC timestamp of consumption.</param>
public sealed record RecordApiConsumptionCommand(
    AdPlatform Platform,
    long Units,
    DateTime? TimestampUtc = null) : ICommand<PlatformQuotaStatusDto>;
