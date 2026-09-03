using Master.Domain.Auditing;

namespace Master.Application.Auditing;

/// <summary>
/// Contrato de repositório imutável para persistência e consulta de eventos na trilha de auditoria global do MasterDb.
/// Como a auditoria é estritamente append-only, não são expostos métodos de atualização ou remoção.
/// </summary>
public interface IMasterAuditRepository
{
    /// <summary>
    /// Adiciona uma nova entrada de auditoria imutável ao catálogo central.
    /// </summary>
    /// <param name="entry">Entidade de auditoria instanciada.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task AddAsync(MasterAuditEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recupera o registro de auditoria por seu identificador único.
    /// </summary>
    /// <param name="id">Identificador do log de auditoria.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A entrada de auditoria ou null se não localizada.</returns>
    Task<MasterAuditEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recupera os registros de auditoria vinculados a um tenant específico, ordenados cronologicamente (mais recentes primeiro).
    /// </summary>
    /// <param name="tenantId">Identificador do tenant.</param>
    /// <param name="take">Quantidade máxima de registros a retornar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Coleção imutável de entradas de auditoria.</returns>
    Task<IReadOnlyList<MasterAuditEntry>> GetByTenantIdAsync(Guid tenantId, int take = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recupera todos os eventos de auditoria executados sob Shadow Mode (impersonação ativa).
    /// </summary>
    /// <param name="take">Quantidade máxima de registros a retornar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Coleção de registros com intervenções de SuperAdmin.</returns>
    Task<IReadOnlyList<MasterAuditEntry>> GetImpersonatedLogsAsync(int take = 100, CancellationToken cancellationToken = default);
}
