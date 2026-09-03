using BuildingBlocks.Domain.Primitives;

namespace Master.Application.Billing.Dunning;

/// <summary>
/// Service contract for orchestrating progressive dunning assessment cycles.
/// </summary>
public interface IDunningEngineService
{
    /// <summary>
    /// Executes a dunning evaluation cycle against overdue tenants in the master catalog.
    /// </summary>
    /// <param name="referenceDateUtc">Optional explicit UTC reference timestamp for evaluation. Defaults to DateTime.UtcNow.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing summary metrics of the completed cycle or an error.</returns>
    Task<Result<DunningExecutionSummary>> ProcessDunningCycleAsync(
        DateTime? referenceDateUtc = null,
        CancellationToken cancellationToken = default);
}
