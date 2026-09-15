using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Repositories;
using Master.Domain.Tenants;

namespace Master.Application.Tenants.Queries.GetTenantCustomDomain;

/// <summary>
/// Manipulador da consulta <see cref="GetTenantCustomDomainQuery"/>.
/// </summary>
public sealed class GetTenantCustomDomainQueryHandler : IQueryHandler<GetTenantCustomDomainQuery, TenantCustomDomainDto>
{
    private const string DefaultExpectedCnameTarget = "cname.admetricspro.com";

    private readonly ITenantRepository _tenantRepository;
    private readonly IPlanRepository _planRepository;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GetTenantCustomDomainQueryHandler"/>.
    /// </summary>
    /// <param name="tenantRepository">Repositório de tenants do catálogo MasterDb.</param>
    /// <param name="planRepository">Repositório de planos de assinatura.</param>
    public GetTenantCustomDomainQueryHandler(
        ITenantRepository tenantRepository,
        IPlanRepository planRepository)
    {
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _planRepository = planRepository ?? throw new ArgumentNullException(nameof(planRepository));
    }

    /// <inheritdoc />
    public async Task<Result<TenantCustomDomainDto>> Handle(
        GetTenantCustomDomainQuery query,
        CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(new TenantId(query.TenantId), cancellationToken);
        if (tenant is null)
        {
            return Result<TenantCustomDomainDto>.Failure(
                Error.NotFound("Tenant.NotFound", "Inquilino não encontrado no catálogo MasterDb."));
        }

        var plan = await _planRepository.GetByTierAsync(tenant.Tier, cancellationToken);
        var hasCustomCname = plan?.Features.HasCustomCname ?? false;

        var dto = new TenantCustomDomainDto(
            tenant.Id.Value,
            tenant.CustomDomain,
            DefaultExpectedCnameTarget,
            !string.IsNullOrWhiteSpace(tenant.CustomDomain),
            hasCustomCname);

        return Result<TenantCustomDomainDto>.Success(dto);
    }
}
