using Master.Domain.Billing;
using Master.Domain.Tenants;

namespace Master.Application.Repositories;

/// <summary>
/// Contrato de persistência para o histórico de transações financeiras e pagamentos de inquilinos (<see cref="TenantPaymentTransaction"/>).
/// </summary>
public interface ITenantPaymentTransactionRepository
{
    /// <summary>
    /// Adiciona uma nova transação financeira ao catálogo Master.
    /// </summary>
    /// <param name="transaction">Entidade da transação a persistir.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Tarefa assíncrona.</returns>
    Task AddAsync(TenantPaymentTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém uma transação financeira pelo seu identificador único global.
    /// </summary>
    /// <param name="id">Identificador único da transação.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Instância da transação se localizada; caso contrário, nulo.</returns>
    Task<TenantPaymentTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém uma transação financeira pelo identificador externo atribuído pelo gateway de pagamentos.
    /// </summary>
    /// <param name="gatewayTransactionId">Identificador único externo da cobrança.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Instância da transação se localizada; caso contrário, nulo.</returns>
    Task<TenantPaymentTransaction?> GetByGatewayTransactionIdAsync(string gatewayTransactionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista o histórico de transações financeiras vinculadas a um tenant específico ordenadas decrescente por data de criação.
    /// </summary>
    /// <param name="tenantId">Identificador do tenant contratante.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Coleção somente-leitura das transações registradas.</returns>
    Task<IReadOnlyList<TenantPaymentTransaction>> GetByTenantIdAsync(TenantId tenantId, CancellationToken cancellationToken = default);
}
