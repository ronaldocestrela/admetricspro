using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using MediatR;
using Tenants.Application.Branding.DTOs;
using Tenants.Application.Branding.Repositories;
using Tenants.Application.Persistence;

namespace Tenants.Application.Branding.Commands.UpdateTenantBranding;

/// <summary>
/// Manipulador responsável por atualizar ou registrar a identidade visual White-Label do inquilino no banco operacional.
/// </summary>
public sealed class UpdateTenantBrandingCommandHandler : ICommandHandler<UpdateTenantBrandingCommand, TenantBrandingDetailsDto>
{
    private readonly ITenantBrandingRepository _brandingRepository;
    private readonly ITenantBrandAssetStore _brandAssetStore;
    private readonly ITenantBrandPublicUrlResolver _urlResolver;
    private readonly ITenantUnitOfWork _unitOfWork;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly IPublisher _publisher;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="UpdateTenantBrandingCommandHandler"/>.
    /// </summary>
    /// <param name="brandingRepository">Repositório de identidade visual do inquilino.</param>
    /// <param name="brandAssetStore">Store de ativos de marca.</param>
    /// <param name="urlResolver">Resolvedor de URL pública.</param>
    /// <param name="unitOfWork">Unidade de trabalho do banco de dados dedicado do inquilino.</param>
    /// <param name="tenantContextAccessor">Acessor de contexto multi-inquilino.</param>
    /// <param name="publisher">Publicador in-memory para notificações desacopladas.</param>
    public UpdateTenantBrandingCommandHandler(
        ITenantBrandingRepository brandingRepository,
        ITenantBrandAssetStore brandAssetStore,
        ITenantBrandPublicUrlResolver urlResolver,
        ITenantUnitOfWork unitOfWork,
        ITenantContextAccessor tenantContextAccessor,
        IPublisher publisher)
    {
        _brandingRepository = brandingRepository ?? throw new ArgumentNullException(nameof(brandingRepository));
        _brandAssetStore = brandAssetStore ?? throw new ArgumentNullException(nameof(brandAssetStore));
        _urlResolver = urlResolver ?? throw new ArgumentNullException(nameof(urlResolver));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
    }

    /// <inheritdoc />
    public async Task<Result<TenantBrandingDetailsDto>> Handle(
        UpdateTenantBrandingCommand request,
        CancellationToken cancellationToken)
    {
        var tenantContext = _tenantContextAccessor.TenantContext;
        if (!tenantContext.IsResolved || !tenantContext.TenantId.HasValue || tenantContext.TenantId.Value == Guid.Empty)
        {
            return Result<TenantBrandingDetailsDto>.Failure(
                Error.Unauthorized("Tenant.Unresolved", "Acesso não autorizado: contexto do inquilino não identificado."));
        }

        var tenantId = tenantContext.TenantId.Value;
        var existingBranding = await _brandingRepository.GetAsync(cancellationToken);
        TenantBranding branding;

        if (existingBranding is not null)
        {
            branding = existingBranding;
        }
        else
        {
            var createResult = TenantBranding.Create(
                Guid.NewGuid(),
                request.PrimaryColor,
                request.SecondaryColor);

            if (createResult.IsFailure)
            {
                return Result<TenantBrandingDetailsDto>.Failure(createResult.Error);
            }

            branding = createResult.Value;
            await _brandingRepository.AddAsync(branding, cancellationToken);
        }

        var sync = await TenantBrandingAssetUrlSynchronizer.SyncAsync(
            branding,
            tenantId,
            request.LightLogoUrl,
            request.DarkLogoUrl,
            request.FaviconUrl,
            _urlResolver,
            _brandAssetStore,
            request.RequestOrigin,
            cancellationToken);

        if (sync.IsFailure)
        {
            return Result<TenantBrandingDetailsDto>.Failure(sync.Error);
        }

        var urls = TenantBrandingAssetUrlSynchronizer.GetEffectiveUrls(branding);
        var updateResult = branding.UpdateDetails(
            request.PrimaryColor,
            request.SecondaryColor,
            urls.Light,
            urls.Dark,
            urls.Favicon);

        if (updateResult.IsFailure)
        {
            return Result<TenantBrandingDetailsDto>.Failure(updateResult.Error);
        }

        await _unitOfWork.CommitAsync(cancellationToken);

        var dto = TenantBrandingDetailsMapper.ToDetailsDto(
            branding,
            tenantId,
            _urlResolver,
            request.RequestOrigin);

        return Result<TenantBrandingDetailsDto>.Success(dto);
    }
}
