using Master.Application.Billing.Trial;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Master.Infrastructure.Services;

/// <summary>
/// Serviço em segundo plano hospedado (BackgroundService) que executa periodicamente a avaliação da régua de trial e despacho de notificações transacionais.
/// </summary>
public sealed class TrialNoticeBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<TrialNoticeOptions> _options;
    private readonly ILogger<TrialNoticeBackgroundService> _logger;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TrialNoticeBackgroundService"/>.
    /// </summary>
    /// <param name="scopeFactory">Fábrica de escopos para resolução isolada de dependências com ciclo Scoped.</param>
    /// <param name="options">Monitor de opções reativo para configurações de trial.</param>
    /// <param name="logger">Logger estruturado.</param>
    public TrialNoticeBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<TrialNoticeOptions> options,
        ILogger<TrialNoticeBackgroundService> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Trial Notice Background Service inicializando...");

        var isStartup = true;

        while (!stoppingToken.IsCancellationRequested)
        {
            var options = _options.CurrentValue;

            if (options.Enabled && (options.RunOnStartup || !isStartup))
            {
                try
                {
                    await RunTrialCycleAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro inesperado ao executar o ciclo automatizado de notificações de trial.");
                }
            }
            else if (!options.Enabled)
            {
                _logger.LogDebug("Trial Notice Background Service desativado por configuração.");
            }

            isStartup = false;
            var intervalHours = Math.Max(1, options.IntervalHours);

            try
            {
                await Task.Delay(TimeSpan.FromHours(intervalHours), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("Trial Notice Background Service finalizado.");
    }

    private async Task RunTrialCycleAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var engineService = scope.ServiceProvider.GetRequiredService<ITrialNotificationEngineService>();

        _logger.LogInformation("Disparando ciclo agendado de avaliações da régua de trial...");
        var result = await engineService.ProcessTrialNoticesCycleAsync(null, cancellationToken);

        if (result.IsSuccess)
        {
            var summary = result.Value;
            _logger.LogInformation(
                "Ciclo automatizado de trial finalizado com sucesso. Avaliados: {Evaluated}, Total Enviados: {Sent}, Falhas: {Failures}",
                summary.EvaluatedCount,
                summary.TotalNoticesSent,
                summary.FailuresCount);
        }
        else
        {
            _logger.LogWarning(
                "Ciclo automatizado de trial finalizado com falha: {ErrorCode} - {ErrorMessage}",
                result.Error.Code,
                result.Error.Description);
        }
    }
}
