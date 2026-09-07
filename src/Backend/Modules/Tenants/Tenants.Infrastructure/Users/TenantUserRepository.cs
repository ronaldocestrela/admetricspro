using BuildingBlocks.Domain.Tenants;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Tenants.Application.Users.Repositories;

namespace Tenants.Infrastructure.Users;

/// <summary>
/// Implementação de <see cref="ITenantUserRepository"/> para operações com colaboradores no banco dedicado do inquilino.
/// </summary>
public sealed class TenantUserRepository : ITenantUserRepository
{
    private readonly ITenantDbContextAccessor _contextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TenantUserRepository"/>.
    /// </summary>
    /// <param name="contextAccessor">Acessor de contexto para obter o TenantDbContext.</param>
    public TenantUserRepository(ITenantDbContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
    }

    /// <inheritdoc />
    public async Task<TenantUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return null;
        }

        return await contextResult.Value.TenantUsers
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TenantUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return null;
        }

        var normalized = email.Trim().ToLower();
        return await contextResult.Value.TenantUsers
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalized, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return false;
        }

        return await contextResult.Value.TenantUsers
            .AnyAsync(u => u.Id == id && u.IsActive, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TenantUser>> GetAllAsync(bool? activeOnly = null, CancellationToken cancellationToken = default)
    {
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return Array.Empty<TenantUser>();
        }

        var query = contextResult.Value.TenantUsers.AsNoTracking();
        if (activeOnly.HasValue)
        {
            query = query.Where(u => u.IsActive == activeOnly.Value);
        }

        return await query.OrderBy(u => u.FullName).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(TenantUser user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsSuccess)
        {
            await contextResult.Value.TenantUsers.AddAsync(user, cancellationToken);
        }
    }

    /// <inheritdoc />
    public void Update(TenantUser user)
    {
        ArgumentNullException.ThrowIfNull(user);
        var contextResult = _contextAccessor.GetDbContextAsync().GetAwaiter().GetResult();
        if (contextResult.IsSuccess)
        {
            contextResult.Value.TenantUsers.Update(user);
        }
    }
}
