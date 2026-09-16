using Automations.Application.Persistence;
using BuildingBlocks.Infrastructure.Persistence;

namespace Automations.Infrastructure.Persistence;

/// <summary>
/// Implementação de <see cref="IAutomationsUnitOfWork"/> que consolida alterações no banco de dados do inquilino corrente.
/// </summary>
public sealed class AutomationsUnitOfWork : IAutomationsUnitOfWork
{
    private readonly ITenantDbContextAccessor _contextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="AutomationsUnitOfWork"/>.
    /// </summary>
    /// <param name="contextAccessor">Acessor de contexto do tenant ativo.</param>
    public AutomationsUnitOfWork(ITenantDbContextAccessor contextAccessor)
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
