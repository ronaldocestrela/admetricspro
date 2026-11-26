using BuildingBlocks.Infrastructure.Storage;
using Microsoft.Extensions.Options;
using SocialPublishing.Application.Options;
using SocialPublishing.Infrastructure.Media;

namespace WebApi.Background;

/// <summary>
/// Varre staging orgânico local periodicamente quando <see cref="ObjectStorageOptions.Provider"/> é <c>Local</c>.
/// </summary>
public sealed class OrganicMediaStagingSweepHostedService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);
    private readonly OrganicMediaStagingSweeper _sweeper;
    private readonly ILogger<OrganicMediaStagingSweepHostedService> _logger;
    private readonly ObjectStorageOptions _objectStorageOptions;

    /// <summary>
    /// Inicializa o worker de limpeza.
    /// </summary>
    /// <param name="mediaOptions">Opções de mídia orgânica.</param>
    /// <param name="objectStorageOptions">Opções de object storage.</param>
    /// <param name="timeProvider">Relógio da aplicação.</param>
    /// <param name="logger">Logger estruturado.</param>
    public OrganicMediaStagingSweepHostedService(
        IOptions<OrganicMediaOptions> mediaOptions,
        IOptions<ObjectStorageOptions> objectStorageOptions,
        TimeProvider timeProvider,
        ILogger<OrganicMediaStagingSweepHostedService> logger)
    {
        _objectStorageOptions = objectStorageOptions?.Value ?? throw new ArgumentNullException(nameof(objectStorageOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sweeper = new OrganicMediaStagingSweeper(mediaOptions?.Value ?? throw new ArgumentNullException(nameof(mediaOptions)), timeProvider);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (ObjectStorageOptions.ShouldRegisterMinioHealthCheck(_objectStorageOptions))
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var deleted = await _sweeper.SweepAsync(stoppingToken);
                if (deleted > 0)
                {
                    _logger.LogInformation("Staging orgânico local: {Count} arquivo(s) removido(s).", deleted);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Falha na varredura de staging orgânico local.");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}