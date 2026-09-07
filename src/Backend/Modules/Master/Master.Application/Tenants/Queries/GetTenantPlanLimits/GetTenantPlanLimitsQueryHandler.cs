using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Tenants.Queries.GetTenantPlanLimits;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Repositories;
using Master.Domain.Plans;
using Master.Domain.Tenants;

namespace Master.Application.Tenants.Queries.GetTenantPlanLimits;

/// <summary>
/// Manipulador da consulta <see cref="GetTenantPlanLimitsQuery"/> que busca os limites de plano do inquilino.
/// </summary>
public sealed class GetTenantPlanLimitsQueryHandler : IQueryHandler<GetTenantPlanLimitsQuery, TenantPlanLimitsDto>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IPlanRepository _planRepository;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GetTenantPlanLimitsQueryHandler"/>.
    /// </summary>
    /// <param name="tenantRepository">Repositório de inquilinos do catálogo Master.</param>
    /// <param name="planRepository">Repositório de planos de assinatura.</param>
    public GetTenantPlanLimitsQueryHandler(
        ITenantRepository tenantRepository,
        IPlanRepository planRepository)
    {
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _planRepository = planRepository ?? throw new ArgumentNullException(nameof(planRepository));
    }

    /// <inheritdoc />
    public async Task<Result<TenantPlanLimitsDto>> Handle(
        GetTenantPlanLimitsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.TenantId == Guid.Empty)
        {
            return Result<TenantPlanLimitsDto>.Failure(
                Error.Validation("Tenant.InvalidId", "O identificador do inquilino não pode ser vazio."));
        }

        var tenant = await _tenantRepository.GetByIdAsync(new TenantId(request.TenantId), cancellationToken);
        if (tenant is null)
        {
            return Result<TenantPlanLimitsDto>.Failure(
                Error.NotFound("Tenant.NotFound", $"Inquilino com identificador '{request.TenantId}' não encontrado no catálogo."));
        }

        var plan = await _planRepository.GetByTierAsync(tenant.Tier, cancellationToken);
        if (plan is not null)
        {
            return Result<TenantPlanLimitsDto>.Success(new TenantPlanLimitsDto(
                tenant.Tier.ToString(),
                plan.Limits.MaxWorkspaces,
                plan.Limits.MaxSeats,
                plan.Limits.MonthlyAdSpendCap));
        }

        // Fallback padrão baseado no enum de tiers caso o catálogo ainda não possua o plano parametrizado
        var fallbackLimits = ResolveDefaultLimitsForTier(tenant.Tier);
        return Result<TenantPlanLimitsDto>.Success(fallbackLimits);
    }

    private static TenantPlanLimitsDto ResolveDefaultLimitsForTier(SubscriptionTier tier)
    {
        return tier switch
        {
            SubscriptionTier.Trial => new TenantPlanLimitsDto(tier.ToString(), MaxWorkspaces: 3, MaxSeats: 2, MonthlyAdSpendCap: 20_000m),
            SubscriptionTier.Starter => new TenantPlanLimitsDto(tier.ToString(), MaxWorkspaces: 3, MaxSeats: 3, MonthlyAdSpendCap: 50_000m),
            SubscriptionTier.Pro => new TenantPlanLimitsDto(tier.ToString(), MaxWorkspaces: 15, MaxSeats: 10, MonthlyAdSpendCap: 250_000m),
            SubscriptionTier.Enterprise => new TenantPlanLimitsDto(tier.ToString(), MaxWorkspaces: int.MaxValue, MaxSeats: int.MaxValue, MonthlyAdSpendCap: decimal.MaxValue),
            _ => new TenantPlanLimitsDto(tier.ToString(), MaxWorkspaces: 3, MaxSeats: 1, MonthlyAdSpendCap: 10_000m)
        };
    }
}
