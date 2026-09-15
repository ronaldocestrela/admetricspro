using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Integrations.Application.OAuth.DTOs;
using Integrations.Domain.OAuth;

namespace Integrations.Application.OAuth.Queries.GetOAuthConnectionsStatus;

/// <summary>
/// Manipulador da consulta <see cref="GetOAuthConnectionsStatusQuery"/>.
/// Retorna metadados operacionais e validade dos tokens do workspace mascarando os segredos.
/// </summary>
public sealed class GetOAuthConnectionsStatusQueryHandler
    : IQueryHandler<GetOAuthConnectionsStatusQuery, IReadOnlyList<OAuthConnectionStatusDto>>
{
    private static readonly TimeSpan ExpirationWarningThreshold = TimeSpan.FromHours(72);
    private readonly IOAuthTokenVaultRepository _vaultRepository;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GetOAuthConnectionsStatusQueryHandler"/>.
    /// </summary>
    public GetOAuthConnectionsStatusQueryHandler(IOAuthTokenVaultRepository vaultRepository)
    {
        _vaultRepository = vaultRepository ?? throw new ArgumentNullException(nameof(vaultRepository));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<OAuthConnectionStatusDto>>> Handle(
        GetOAuthConnectionsStatusQuery request,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Result<IReadOnlyList<OAuthConnectionStatusDto>>.Failure(
                Error.Validation("GetOAuthStatus.EmptyWorkspaceId", "O identificador do workspace é obrigatório."));
        }

        var list = await _vaultRepository.GetByWorkspaceIdAsync(request.WorkspaceId, cancellationToken);

        var dtos = list.Select(v => new OAuthConnectionStatusDto(
            v.Id,
            v.WorkspaceId,
            v.Platform,
            v.ExternalAccountId,
            v.ExternalAccountName,
            v.Status,
            v.Scopes,
            v.AccessTokenExpiresAtUtc,
            v.IsExpiringSoon(ExpirationWarningThreshold),
            v.CreatedAtUtc,
            v.UpdatedAtUtc)).ToList();

        return Result<IReadOnlyList<OAuthConnectionStatusDto>>.Success(dtos);
    }
}
