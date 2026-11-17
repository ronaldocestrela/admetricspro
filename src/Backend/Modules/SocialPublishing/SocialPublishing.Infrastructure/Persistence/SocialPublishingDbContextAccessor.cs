using BuildingBlocks.Domain.Primitives;

namespace SocialPublishing.Infrastructure.Persistence;

/// <summary>
/// Reutiliza uma instância scoped de <see cref="SocialPublishingDbContext"/> por requisição ou escopo de worker.
/// </summary>
public sealed class SocialPublishingDbContextAccessor : IAsyncDisposable, IDisposable
{
    private readonly SocialPublishingDbContextFactory _contextFactory;
    private SocialPublishingDbContext? _dbContext;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="SocialPublishingDbContextAccessor"/>.
    /// </summary>
    /// <param name="contextFactory">Fábrica de contexto do módulo SocialPublishing.</param>
    public SocialPublishingDbContextAccessor(SocialPublishingDbContextFactory contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    /// <summary>
    /// Obtém o contexto do tenant corrente, criando-o na primeira chamada do escopo.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Contexto configurado ou falha de resolução.</returns>
    public async Task<Result<SocialPublishingDbContext>> GetDbContextAsync(CancellationToken cancellationToken = default)
    {
        if (_dbContext is not null)
        {
            return Result<SocialPublishingDbContext>.Success(_dbContext);
        }

        var result = await _contextFactory.CreateDbContextAsync(cancellationToken);
        if (result.IsFailure)
        {
            return result;
        }

        _dbContext = result.Value;
        return Result<SocialPublishingDbContext>.Success(_dbContext);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _dbContext?.Dispose();
        _dbContext = null;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_dbContext is not null)
        {
            await _dbContext.DisposeAsync();
            _dbContext = null;
        }
    }
}
