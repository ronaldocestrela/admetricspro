using Master.Domain.Tenants;

namespace Master.Application.Repositories;

/// <summary>
/// Contrato de repositório para persistência e consulta de logs de notificações transacionais de inquilinos.
/// </summary>
public interface ITenantNotificationLogRepository
{
    /// <summary>
    /// Adiciona uma nova entrada de log de notificação no catálogo.
    /// </summary>
    /// <param name="log">Entidade de log validada.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Tarefa assíncrona.</returns>
    Task AddAsync(TenantNotificationLog log, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta o conjunto de tipos de notificações já enviadas com sucesso para determinado inquilino.
    /// </summary>
    /// <param name="tenantId">Identificador do inquilino.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Conjunto somente-leitura dos tipos de avisos já despachados.</returns>
    Task<IReadOnlySet<TrialNoticeType>> GetSentNoticeTypesForTenantAsync(TenantId tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém todo o histórico de logs de notificações de um inquilino ordenado por data decrescente.
    /// </summary>
    /// <param name="tenantId">Identificador do inquilino.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista somente-leitura dos registros de notificação.</returns>
    Task<IReadOnlyList<TenantNotificationLog>> GetLogsByTenantIdAsync(TenantId tenantId, CancellationToken cancellationToken = default);
}
