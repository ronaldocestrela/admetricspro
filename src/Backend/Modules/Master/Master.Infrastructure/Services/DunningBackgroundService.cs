using Master.Application.Billing.Dunning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Master.Infrastructure.Services;

/// <summary>
/// Background hosted service that executes periodic dunning assessment cycles against overdue tenants.
/// </summary>
public sealed class DunningBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<DunningOptions> _options;
    private readonly ILogger<DunningBackgroundService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DunningBackgroundService"/> class.
    /// </summary>
    /// <param name="scopeFactory">Scope factory for resolving scoped application services.</param>
    /// <param name="options">Dunning service options monitor.</param>
    /// <param name="logger">Structured logger instance.</param>
    public DunningBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<DunningOptions> options,
        ILogger<DunningBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Dunning Background Service starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            var options = _options.CurrentValue;

            if (options.Enabled)
            {
                try
                {
                    await RunDunningCycleAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error occurred while executing automated dunning cycle.");
                }
            }
            else
            {
                _logger.LogDebug("Dunning Background Service is disabled by configuration.");
            }

            var intervalMinutes = Math.Max(1, options.IntervalMinutes);
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("Dunning Background Service stopping.");
    }

    private async Task RunDunningCycleAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dunningEngine = scope.ServiceProvider.GetRequiredService<IDunningEngineService>();

        _logger.LogInformation("Triggering scheduled dunning evaluation cycle...");
        var result = await dunningEngine.ProcessDunningCycleAsync(null, cancellationToken);

        if (result.IsSuccess)
        {
            var summary = result.Value;
            _logger.LogInformation(
                "Automated dunning cycle succeeded. Evaluated: {Evaluated}, Transitions: {Transitions}, Suspended: {Suspended}",
                summary.EvaluatedCount,
                summary.TransitionsCount,
                summary.SuspendedCount);
        }
        else
        {
            _logger.LogWarning(
                "Automated dunning cycle completed with failure: {ErrorCode} - {ErrorMessage}",
                result.Error.Code,
                result.Error.Description);
        }
    }
}
