using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;

namespace Master.Application.Billing.Dunning;

/// <summary>
/// Handles <see cref="ExecuteDunningCycleCommand"/> by dispatching the cycle through <see cref="IDunningEngineService"/>.
/// </summary>
public sealed class ExecuteDunningCycleCommandHandler : ICommandHandler<ExecuteDunningCycleCommand, DunningExecutionSummary>
{
    private readonly IDunningEngineService _dunningEngineService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecuteDunningCycleCommandHandler"/> class.
    /// </summary>
    /// <param name="dunningEngineService">Dunning engine processing service.</param>
    public ExecuteDunningCycleCommandHandler(IDunningEngineService dunningEngineService)
    {
        _dunningEngineService = dunningEngineService;
    }

    /// <inheritdoc />
    public Task<Result<DunningExecutionSummary>> Handle(ExecuteDunningCycleCommand request, CancellationToken cancellationToken)
    {
        return _dunningEngineService.ProcessDunningCycleAsync(request.ReferenceDateUtc, cancellationToken);
    }
}
