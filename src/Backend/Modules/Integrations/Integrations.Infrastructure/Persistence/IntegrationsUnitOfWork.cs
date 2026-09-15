using BuildingBlocks.Infrastructure.Persistence;
using Integrations.Application.Persistence;

namespace Integrations.Infrastructure.Persistence;

/// <summary>
/// Implementação de <see cref="IIntegrationsUnitOfWork"/> que consolida alterações pendentes
/// na instância ativa do <see cref="TenantDbContext"/>.
/// </summary>
public sealed class IntegrationsUnitOfWork : IIntegrationsUnitOfWork
{
    private readonly ITenantDbContextAccessor _contextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="IntegrationsUnitOfWork"/>.
    /// </summary>
    /// <param name="contextAccessor">Acessor do contexto de banco do inquilino corrente.</param>
    public IntegrationsUnitOfWork(ITenantDbContextAccessor contextAccessor)
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
