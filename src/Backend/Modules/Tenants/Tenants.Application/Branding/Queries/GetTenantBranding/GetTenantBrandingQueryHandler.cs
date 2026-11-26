using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using Tenants.Application.Branding.DTOs;
using Tenants.Application.Branding.Repositories;

namespace Tenants.Application.Branding.Queries.GetTenantBranding;

/// <summary>
/// Manipulador da consulta <see cref="GetTenantBrandingQuery"/> que retorna a identidade visual configurada ou padrão.
/// </summary>
public sealed class GetTenantBrandingQueryHandler : IQueryHandler<GetTenantBrandingQuery, TenantBrandingDetailsDto>
{
    private const string DefaultPrimaryColor = "#2563EB";
    private const string DefaultSecondaryColor = "#0F172A";

    private readonly ITenantBrandingRepository _brandingRepository;
    private readonly ITenantBrandPublicUrlResolver _urlResolver;
    private readonly ITenantContextAccessor _tenantContextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GetTenantBrandingQueryHandler"/>.
    /// </summary>
    /// <param name="brandingRepository">Repositório de branding do inquilino.</param>
    /// <param name="urlResolver">Resolvedor de URL pública.</param>
    /// <param name="tenantContextAccessor">Acessor do contexto de inquilino ativo.</param>
    public GetTenantBrandingQueryHandler(
        ITenantBrandingRepository brandingRepository,
        ITenantBrandPublicUrlResolver urlResolver,
        ITenantContextAccessor tenantContextAccessor)
    {
        _brandingRepository = brandingRepository ?? throw new ArgumentNullException(nameof(brandingRepository));
        _urlResolver = urlResolver ?? throw new ArgumentNullException(nameof(urlResolver));
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
    }

    /// <inheritdoc />
    public async Task<Result<TenantBrandingDetailsDto>> Handle(
        GetTenantBrandingQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContextAccessor.TenantContext?.TenantId ?? Guid.Empty;
        var branding = await _brandingRepository.GetAsync(cancellationToken);
        if (branding is null)
        {
            return Result<TenantBrandingDetailsDto>.Success(new TenantBrandingDetailsDto(
                PrimaryColor: DefaultPrimaryColor,
                SecondaryColor: DefaultSecondaryColor,
                LightLogoUrl: null,
                DarkLogoUrl: null,
                FaviconUrl: null,
                UpdatedAtUtc: null));
        }

        var dto = TenantBrandingDetailsMapper.ToDetailsDto(
            branding,
            tenantId,
            _urlResolver,
            request.RequestOrigin);

        return Result<TenantBrandingDetailsDto>.Success(dto);
    }
}
