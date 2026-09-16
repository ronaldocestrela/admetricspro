using BuildingBlocks.Domain.Automations.SafetyGuards;

namespace Automations.Domain.SafetyGuards;

/// <summary>
/// Contrato de repositório para persistência e consulta de incidentes de travas de segurança operacional.
/// Opera no contexto do banco de dados dedicado do inquilino (TenantDbContext).
/// </summary>
public interface ISafetyGuardIncidentRepository
{
    /// <summary>
    /// Adiciona um novo incidente de segurança no banco de dados.
    /// </summary>
    /// <param name="incident">Instância validada do incidente.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task AddAsync(SafetyGuardIncident incident, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém os incidentes recentes registrados para um workspace específico ordenados por data decrescente.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="limit">Quantidade máxima de registros a retornar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Coleção de incidentes localizados.</returns>
    Task<IReadOnlyList<SafetyGuardIncident>> GetRecentByWorkspaceIdAsync(
        Guid workspaceId,
        int limit = 50,
        CancellationToken cancellationToken = default);
}
