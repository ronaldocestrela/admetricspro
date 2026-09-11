using BuildingBlocks.Domain.Tenants;

namespace Tenants.Application.Audit.DTOs;

/// <summary>
/// Objeto de transferência de dados representando uma entrada da trilha de auditoria do inquilino.
/// </summary>
/// <param name="Id">Identificador do log de auditoria.</param>
/// <param name="UserId">Identificador do operador.</param>
/// <param name="UserEmail">Email do operador no momento da ação.</param>
/// <param name="Action">Ação auditada executada.</param>
/// <param name="Resource">Entidade afetada.</param>
/// <param name="ResourceId">Identificador textual do recurso afetado.</param>
/// <param name="Details">Detalhes ou descrição contextual da operação.</param>
/// <param name="IpAddress">Endereço IP da requisição.</param>
/// <param name="CreatedAtUtc">Data e hora UTC em que o registro foi gravado.</param>
public sealed record TenantAuditLogDto(
    Guid Id,
    Guid UserId,
    string UserEmail,
    string Action,
    string Resource,
    string? ResourceId,
    string? Details,
    string? IpAddress,
    DateTime CreatedAtUtc)
{
    /// <summary>
    /// Mapeia uma entidade <see cref="TenantAuditLog"/> para seu respectivo DTO de apresentação.
    /// </summary>
    /// <param name="entity">Entidade persistida.</param>
    /// <returns>DTO preenchido.</returns>
    public static TenantAuditLogDto FromEntity(TenantAuditLog entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new TenantAuditLogDto(
            entity.Id,
            entity.UserId,
            entity.UserEmail,
            entity.Action,
            entity.Resource,
            entity.ResourceId,
            entity.Details,
            entity.IpAddress,
            entity.CreatedAtUtc);
    }
}
