using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Connections.DTOs;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Connections;

namespace SocialPublishing.Application.Connections.Queries.ListOrganicConnections;

/// <summary>
/// Manipulador da consulta <see cref="ListOrganicConnectionsQuery"/>.
/// </summary>
public sealed class ListOrganicConnectionsQueryHandler
    : IQueryHandler<ListOrganicConnectionsQuery, IReadOnlyList<OrganicConnectionSummaryDto>>
{
    private readonly IOrganicConnectionRepository _connectionRepository;
    private readonly ITenantContextAccessor _tenantContextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ListOrganicConnectionsQueryHandler"/>.
    /// </summary>
    /// <param name="connectionRepository">Repositório de conexões orgânicas.</param>
    /// <param name="tenantContextAccessor">Contexto de tenant da requisição.</param>
    public ListOrganicConnectionsQueryHandler(
        IOrganicConnectionRepository connectionRepository,
        ITenantContextAccessor tenantContextAccessor)
    {
        _connectionRepository = connectionRepository ?? throw new ArgumentNullException(nameof(connectionRepository));
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<OrganicConnectionSummaryDto>>> Handle(
        ListOrganicConnectionsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Result<IReadOnlyList<OrganicConnectionSummaryDto>>.Failure(SocialErrors.WorkspaceRequired);
        }

        var tenantId = _tenantContextAccessor.TenantContext?.TenantId;
        if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
        {
            return Result<IReadOnlyList<OrganicConnectionSummaryDto>>.Failure(SocialErrors.TenantRequired);
        }

        var connections = await _connectionRepository.ListByWorkspaceAsync(request.WorkspaceId, cancellationToken);
        var summaries = connections
            .OrderBy(c => c.Platform)
            .ThenBy(c => c.DisplayName)
            .Select(MapToSummary)
            .ToList();

        return Result<IReadOnlyList<OrganicConnectionSummaryDto>>.Success(summaries);
    }

    private static OrganicConnectionSummaryDto MapToSummary(OrganicConnection connection) =>
        new(
            connection.Id,
            connection.WorkspaceId,
            connection.Platform,
            connection.DisplayName,
            connection.ExternalAccountId,
            connection.Status,
            connection.AccessTokenExpiresAtUtc);
}
