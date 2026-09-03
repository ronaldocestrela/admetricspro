using BuildingBlocks.Domain.Primitives;

namespace Master.Application.Auditing;

/// <summary>
/// Serviço de alto nível para registro centralizado de auditoria operacional no Catálogo Master.
/// Automaticamente detecta o contexto de Shadow Mode via <see cref="BuildingBlocks.Application.Security.IImpersonationContext"/>,
/// aplicando a tag 'performed_by_superadmin' e associando o ticket de suporte e o SuperAdmin responsável.
/// </summary>
public interface IMasterAuditService
{
    /// <summary>
    /// Registra uma ação executada no sistema, enriquecendo o registro com dados contextuais de impersonação quando aplicável.
    /// </summary>
    /// <param name="action">Ação executada (ex.: "Workspace.UpdateBudgetLimit", "Tenant.Suspend").</param>
    /// <param name="resource">Tipo de recurso (ex.: "Workspace", "Tenant", "Campaign").</param>
    /// <param name="resourceId">Identificador do recurso.</param>
    /// <param name="details">Carga textual informativa ou justificativa.</param>
    /// <param name="tenantId">Identificador do tenant, se aplicável.</param>
    /// <param name="ipAddress">Endereço IP de origem.</param>
    /// <param name="additionalTags">Tags semânticas complementares.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado contendo o ID do registro de auditoria gravado ou falha.</returns>
    Task<Result<Guid>> RecordAsync(
        string action,
        string resource,
        string? resourceId = null,
        string? details = null,
        Guid? tenantId = null,
        string? ipAddress = null,
        IEnumerable<string>? additionalTags = null,
        CancellationToken cancellationToken = default);
}
