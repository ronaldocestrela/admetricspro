using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Integrations.DTOs;
using Master.Application.Integrations.Services;

namespace Master.Application.Integrations.Commands.RecordApiConsumption;

/// <summary>
/// Command handler that records API usage against the quota tracker service.
/// </summary>
public sealed class RecordApiConsumptionCommandHandler : ICommandHandler<RecordApiConsumptionCommand, PlatformQuotaStatusDto>
{
    private readonly IApiQuotaTrackerService _quotaTrackerService;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecordApiConsumptionCommandHandler"/> class.
    /// </summary>
    /// <param name="quotaTrackerService">Quota tracker service.</param>
    public RecordApiConsumptionCommandHandler(IApiQuotaTrackerService quotaTrackerService)
    {
        _quotaTrackerService = quotaTrackerService ?? throw new ArgumentNullException(nameof(quotaTrackerService));
    }

    /// <inheritdoc />
    public async Task<Result<PlatformQuotaStatusDto>> Handle(
        RecordApiConsumptionCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var timestamp = command.TimestampUtc ?? DateTime.UtcNow;

        return await _quotaTrackerService.RecordUsageAsync(
            command.Platform,
            command.Units,
            timestamp,
            cancellationToken);
    }
}
