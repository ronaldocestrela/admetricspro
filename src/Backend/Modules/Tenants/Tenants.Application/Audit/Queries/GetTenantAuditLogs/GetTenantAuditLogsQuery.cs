using BuildingBlocks.Application.Messaging;
using Tenants.Application.Audit.DTOs;

namespace Tenants.Application.Audit.Queries.GetTenantAuditLogs;

/// <summary>
/// Consulta para obtenção paginada e filtrada dos registros de auditoria imutável do inquilino.
/// </summary>
/// <param name="UserId">Filtro opcional pelo identificador do operador.</param>
/// <param name="Action">Filtro opcional pelo nome da ação executada.</param>
/// <param name="FromUtc">Filtro opcional por data inicial UTC.</param>
/// <param name="ToUtc">Filtro opcional por data final UTC.</param>
/// <param name="Page">Número da página (padrão: 1).</param>
/// <param name="PageSize">Quantidade de registros por página (padrão: 50).</param>
public sealed record GetTenantAuditLogsQuery(
    Guid? UserId = null,
    string? Action = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    int Page = 1,
    int PageSize = 50) : IQuery<TenantAuditLogsResponse>;
