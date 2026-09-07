using BuildingBlocks.Infrastructure.Persistence;
using Tenants.Application.Persistence;

namespace Tenants.Infrastructure.Persistence;

/// <summary>
/// Implementação de <see cref="ITenantUnitOfWork"/> que consolida alterações pendentes na instância ativa do <see cref="TenantDbContext"/>.
/// </summary>
public sealed class TenantUnitOfWork : ITenantUnitOfWork
{
    private readonly ITenantDbContextAccessor _contextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TenantUnitOfWork"/>.
    /// </summary>
    /// <param name="contextAccessor">Acessor do contexto de banco do inquilino corrente.</param>
    public TenantUnitOfWork(ITenantDbContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
    }

    /// <inheritdoc />
    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return 0;
        }

        return await contextResult.Value.SaveChangesAsync(cancellationToken);
    }
}
