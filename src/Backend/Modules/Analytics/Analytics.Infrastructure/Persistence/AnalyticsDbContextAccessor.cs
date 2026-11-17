using BuildingBlocks.Domain.Primitives;

namespace Analytics.Infrastructure.Persistence;

/// <summary>
/// Reutiliza uma instância scoped de <see cref="AnalyticsDbContext"/> por requisição ou worker.
/// </summary>
public sealed class AnalyticsDbContextAccessor : IAsyncDisposable, IDisposable
{
    private readonly AnalyticsDbContextFactory _contextFactory;
    private AnalyticsDbContext? _dbContext;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="AnalyticsDbContextAccessor"/>.
    /// </summary>
    /// <param name="contextFactory">Fábrica do contexto Analytics.</param>
    public AnalyticsDbContextAccessor(AnalyticsDbContextFactory contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    /// <summary>
    /// Obtém o contexto do tenant corrente.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Contexto ou falha de resolução.</returns>
    public async Task<Result<AnalyticsDbContext>> GetDbContextAsync(CancellationToken cancellationToken = default)
    {
        if (_dbContext is not null)
        {
            return Result<AnalyticsDbContext>.Success(_dbContext);
        }

        var result = await _contextFactory.CreateDbContextAsync(cancellationToken);
        if (result.IsFailure)
        {
            return result;
        }

        _dbContext = result.Value;
        return Result<AnalyticsDbContext>.Success(_dbContext);
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
