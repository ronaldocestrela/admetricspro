using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Tenants.Application.Users.DTOs;
using Tenants.Application.Users.Services;

namespace Tenants.Application.Users.Queries.GetTenantInvitationByToken;

/// <summary>
/// Manipulador de <see cref="GetTenantInvitationByTokenQuery"/>.
/// </summary>
public sealed class GetTenantInvitationByTokenQueryHandler : IQueryHandler<GetTenantInvitationByTokenQuery, TenantInvitationPreviewDto>
{
    private readonly ITenantInvitationPublicGateway _publicGateway;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GetTenantInvitationByTokenQueryHandler"/>.
    /// </summary>
    /// <param name="publicGateway">Gateway de operações públicas de convite.</param>
    public GetTenantInvitationByTokenQueryHandler(ITenantInvitationPublicGateway publicGateway)
    {
        _publicGateway = publicGateway ?? throw new ArgumentNullException(nameof(publicGateway));
    }

    /// <inheritdoc />
    public async Task<Result<TenantInvitationPreviewDto>> Handle(
        GetTenantInvitationByTokenQuery query,
        CancellationToken cancellationToken)
    {
        return await _publicGateway.GetPreviewAsync(query.Token, query.Subdomain, cancellationToken);
    }
}