using BuildingBlocks.Domain.Tenants;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Tenants.Application.Branding.Repositories;

namespace Tenants.Infrastructure.Branding;

/// <summary>
/// Implementação de <see cref="ITenantBrandingRepository"/> utilizando <see cref="ITenantDbContextAccessor"/> para acesso ao banco operacional dedicado.
/// </summary>
public sealed class TenantBrandingRepository : ITenantBrandingRepository
{
    private readonly ITenantDbContextAccessor _contextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TenantBrandingRepository"/>.
    /// </summary>
    /// <param name="contextAccessor">Acessor de contexto para obter o TenantDbContext resolvido.</param>
    public TenantBrandingRepository(ITenantDbContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
    }

    /// <inheritdoc />
    public async Task<TenantBranding?> GetAsync(CancellationToken cancellationToken = default)
    {
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return null;
        }

        return await contextResult.Value.TenantBranding
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(TenantBranding branding, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(branding);

        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return;
        }

        await contextResult.Value.TenantBranding.AddAsync(branding, cancellationToken);
    }
}
