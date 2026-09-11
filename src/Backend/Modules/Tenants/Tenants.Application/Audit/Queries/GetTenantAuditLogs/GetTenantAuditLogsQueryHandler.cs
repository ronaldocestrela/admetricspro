using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Tenants.Application.Audit.DTOs;
using Tenants.Application.Audit.Repositories;

namespace Tenants.Application.Audit.Queries.GetTenantAuditLogs;

/// <summary>
/// Manipulador da consulta <see cref="GetTenantAuditLogsQuery"/> para recuperação de trilha de auditoria.
/// </summary>
public sealed class GetTenantAuditLogsQueryHandler : IQueryHandler<GetTenantAuditLogsQuery, TenantAuditLogsResponse>
{
    private readonly ITenantAuditLogRepository _auditRepository;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GetTenantAuditLogsQueryHandler"/>.
    /// </summary>
    /// <param name="auditRepository">Repositório de auditoria do inquilino.</param>
    public GetTenantAuditLogsQueryHandler(ITenantAuditLogRepository auditRepository)
    {
        _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
    }

    /// <inheritdoc />
    public async Task<Result<TenantAuditLogsResponse>> Handle(
        GetTenantAuditLogsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var safePage = query.Page < 1 ? 1 : query.Page;
        var safePageSize = query.PageSize is < 1 or > 200 ? 50 : query.PageSize;

        var logs = await _auditRepository.GetLogsAsync(
            query.UserId,
            query.Action,
            query.FromUtc,
            query.ToUtc,
            safePage,
            safePageSize,
            cancellationToken);

        var totalCount = await _auditRepository.GetTotalCountAsync(
            query.UserId,
            query.Action,
            query.FromUtc,
            query.ToUtc,
            cancellationToken);

        var dtos = logs.Select(TenantAuditLogDto.FromEntity).ToList();

        var response = new TenantAuditLogsResponse(
            dtos,
            totalCount,
            safePage,
            safePageSize);

        return Result<TenantAuditLogsResponse>.Success(response);
    }
}
