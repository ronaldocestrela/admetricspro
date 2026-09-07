using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Tenants.Application.Auth.DTOs;
using Tenants.Application.Auth.Services;

namespace Tenants.Application.Auth.Queries.GetTenantPublicBranding;

/// <summary>
/// Manipulador da consulta pública de branding do inquilino (<see cref="GetTenantPublicBrandingQuery"/>).
/// Encaminha a resolução para o serviço de governança e autenticação de inquilinos (<see cref="ITenantAuthService"/>).
/// </summary>
public sealed class GetTenantPublicBrandingQueryHandler : IQueryHandler<GetTenantPublicBrandingQuery, TenantPublicBrandingDto>
{
    private readonly ITenantAuthService _tenantAuthService;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GetTenantPublicBrandingQueryHandler"/>.
    /// </summary>
    /// <param name="tenantAuthService">Serviço de autenticação e identificação de inquilinos.</param>
    public GetTenantPublicBrandingQueryHandler(ITenantAuthService tenantAuthService)
    {
        _tenantAuthService = tenantAuthService ?? throw new ArgumentNullException(nameof(tenantAuthService));
    }

    /// <inheritdoc />
    public async Task<Result<TenantPublicBrandingDto>> Handle(
        GetTenantPublicBrandingQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await _tenantAuthService.GetPublicBrandingAsync(query.Subdomain, cancellationToken);
    }
}
