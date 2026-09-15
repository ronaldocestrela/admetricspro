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
    private readonly ITenantContextAccessor _tenantContextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GetTenantBrandingQueryHandler"/>.
    /// </summary>
    /// <param name="brandingRepository">Repositório de branding do inquilino.</param>
    /// <param name="tenantContextAccessor">Acessor do contexto de inquilino ativo.</param>
    public GetTenantBrandingQueryHandler(
        ITenantBrandingRepository brandingRepository,
        ITenantContextAccessor tenantContextAccessor)
    {
        _brandingRepository = brandingRepository ?? throw new ArgumentNullException(nameof(brandingRepository));
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
    }

    /// <inheritdoc />
    public async Task<Result<TenantBrandingDetailsDto>> Handle(
        GetTenantBrandingQuery request,
        CancellationToken cancellationToken)
    {
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

        return Result<TenantBrandingDetailsDto>.Success(new TenantBrandingDetailsDto(
            PrimaryColor: branding.PrimaryColor,
            SecondaryColor: branding.SecondaryColor,
            LightLogoUrl: branding.LightLogoUrl,
            DarkLogoUrl: branding.DarkLogoUrl,
            FaviconUrl: branding.FaviconUrl,
            UpdatedAtUtc: branding.UpdatedAtUtc ?? branding.CreatedAtUtc));
    }
}
