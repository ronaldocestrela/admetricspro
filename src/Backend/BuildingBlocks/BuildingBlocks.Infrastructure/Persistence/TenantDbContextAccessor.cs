using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Implementação de escopo para <see cref="ITenantDbContextAccessor"/> que resolve e reutiliza o <see cref="TenantDbContext"/> da requisição.
/// </summary>
public sealed class TenantDbContextAccessor : ITenantDbContextAccessor, IAsyncDisposable, IDisposable
{
    private readonly ITenantDbContextFactory<TenantDbContext> _contextFactory;
    private TenantDbContext? _dbContext;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TenantDbContextAccessor"/>.
    /// </summary>
    /// <param name="contextFactory">Fábrica dinâmica de contextos de banco do inquilino.</param>
    public TenantDbContextAccessor(ITenantDbContextFactory<TenantDbContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    /// <inheritdoc />
    public async Task<Result<TenantDbContext>> GetDbContextAsync(CancellationToken cancellationToken = default)
    {
        if (_dbContext is not null)
        {
            return Result<TenantDbContext>.Success(_dbContext);
        }

        var result = await _contextFactory.CreateDbContextAsync(cancellationToken);
        if (result.IsFailure)
        {
            return result;
        }

        _dbContext = result.Value;
        return Result<TenantDbContext>.Success(_dbContext);
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
