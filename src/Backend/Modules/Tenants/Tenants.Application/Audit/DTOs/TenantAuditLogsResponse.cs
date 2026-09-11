namespace Tenants.Application.Audit.DTOs;

/// <summary>
/// Resposta paginada contendo a listagem de registros da trilha de auditoria do inquilino.
/// </summary>
/// <param name="Items">Lista de itens retornados na página.</param>
/// <param name="TotalCount">Total de registros que atendem aos filtros especificados.</param>
/// <param name="Page">Número da página atual (1-based).</param>
/// <param name="PageSize">Quantidade máxima de itens por página.</param>
public sealed record TenantAuditLogsResponse(
    IReadOnlyList<TenantAuditLogDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
