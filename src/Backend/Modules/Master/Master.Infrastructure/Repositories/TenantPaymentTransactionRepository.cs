using Master.Application.Repositories;
using Master.Domain.Billing;
using Master.Domain.Tenants;
using Master.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Master.Infrastructure.Repositories;

/// <summary>
/// Implementação EF Core de <see cref="ITenantPaymentTransactionRepository"/> para persistência no catálogo central Master.
/// </summary>
public sealed class TenantPaymentTransactionRepository : ITenantPaymentTransactionRepository
{
    private readonly MasterDbContext _masterDbContext;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TenantPaymentTransactionRepository"/>.
    /// </summary>
    /// <param name="masterDbContext">Contexto do catálogo central Master.</param>
    public TenantPaymentTransactionRepository(MasterDbContext masterDbContext)
    {
        _masterDbContext = masterDbContext ?? throw new ArgumentNullException(nameof(masterDbContext));
    }

    /// <inheritdoc />
    public async Task AddAsync(TenantPaymentTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        await _masterDbContext.PaymentTransactions.AddAsync(transaction, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TenantPaymentTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _masterDbContext.PaymentTransactions.SingleOrDefaultAsync(tx => tx.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TenantPaymentTransaction?> GetByGatewayTransactionIdAsync(string gatewayTransactionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(gatewayTransactionId))
        {
            return Task.FromResult<TenantPaymentTransaction?>(null);
        }

        var normalized = gatewayTransactionId.Trim();
        return _masterDbContext.PaymentTransactions.SingleOrDefaultAsync(tx => tx.GatewayTransactionId == normalized, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TenantPaymentTransaction>> GetByTenantIdAsync(TenantId tenantId, CancellationToken cancellationToken = default)
    {
        var transactions = await _masterDbContext.PaymentTransactions
            .Where(tx => tx.TenantId == tenantId)
            .OrderByDescending(tx => tx.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return transactions;
    }
}
