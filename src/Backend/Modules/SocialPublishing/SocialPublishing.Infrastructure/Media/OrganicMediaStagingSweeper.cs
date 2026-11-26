using SocialPublishing.Application.Options;

namespace SocialPublishing.Infrastructure.Media;

/// <summary>
/// Remove arquivos de staging orgânico locais com mais de 24 horas.
/// </summary>
public sealed class OrganicMediaStagingSweeper
{
    private readonly OrganicMediaOptions _options;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Inicializa o serviço de limpeza de staging.
    /// </summary>
    /// <param name="options">Opções de mídia orgânica.</param>
    /// <param name="timeProvider">Relógio da aplicação.</param>
    public OrganicMediaStagingSweeper(OrganicMediaOptions options, TimeProvider timeProvider)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <summary>
    /// Executa a varredura e apaga arquivos antigos em <c>organic-media/staging/</c>.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Quantidade de arquivos removidos.</returns>
    public Task<int> SweepAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var root = _options.LocalRoot;
        if (string.IsNullOrWhiteSpace(root))
        {
            return Task.FromResult(0);
        }

        var stagingRoot = Path.Combine(root, "organic-media", "staging");
        if (!Directory.Exists(stagingRoot))
        {
            return Task.FromResult(0);
        }

        var threshold = _timeProvider.GetUtcNow().UtcDateTime.AddHours(-24);
        var deleted = 0;

        foreach (var file in Directory.EnumerateFiles(stagingRoot, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var info = new FileInfo(file);
            if (info.LastWriteTimeUtc <= threshold)
            {
                File.Delete(file);
                deleted++;
            }
        }

        return Task.FromResult(deleted);
    }
}