using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Repositories;
using Master.Domain.Tenants;

namespace Master.Application.Billing.Checkout.Queries.GetCheckoutPreview;

/// <summary>
/// Manipulador responsável por projetar os valores líquidos de cobrança, economia e renovação para o checkout.
/// </summary>
public sealed class GetCheckoutPreviewQueryHandler : IQueryHandler<GetCheckoutPreviewQuery, CheckoutPreviewDto>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IPlanRepository _planRepository;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GetCheckoutPreviewQueryHandler"/>.
    /// </summary>
    /// <param name="tenantRepository">Repositório de inquilinos.</param>
    /// <param name="planRepository">Repositório de planos de assinatura.</param>
    public GetCheckoutPreviewQueryHandler(
        ITenantRepository tenantRepository,
        IPlanRepository planRepository)
    {
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _planRepository = planRepository ?? throw new ArgumentNullException(nameof(planRepository));
    }

    /// <inheritdoc />
    public async Task<Result<CheckoutPreviewDto>> Handle(GetCheckoutPreviewQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Tier == SubscriptionTier.Trial)
        {
            return Result<CheckoutPreviewDto>.Failure(
                Error.Validation("Checkout.InvalidTier", "Não é possível realizar checkout para o nível Trial."));
        }

        var isMonthly = string.Equals(request.BillingCycle?.Trim(), "Monthly", StringComparison.OrdinalIgnoreCase);
        var isAnnual = string.Equals(request.BillingCycle?.Trim(), "Annual", StringComparison.OrdinalIgnoreCase);

        if (!isMonthly && !isAnnual)
        {
            return Result<CheckoutPreviewDto>.Failure(
                Error.Validation("Checkout.InvalidBillingCycle", "O ciclo de faturamento deve ser Monthly ou Annual."));
        }

        var tenant = await _tenantRepository.GetByIdAsync(new TenantId(request.TenantId), cancellationToken);
        if (tenant is null)
        {
            return Result<CheckoutPreviewDto>.Failure(
                Error.NotFound("Tenant.NotFound", "Inquilino não localizado no catálogo Master."));
        }

        var plan = await _planRepository.GetByTierAsync(request.Tier, cancellationToken);
        var planName = plan?.Name ?? $"Plano {request.Tier}";
        var monthlyPrice = plan?.MonthlyPrice ?? GetDefaultMonthlyPrice(request.Tier);
        var discountPct = isAnnual ? (plan?.AnnualDiscountPercentage ?? 20) : 0;

        decimal totalPayableNow;
        decimal savingsAmount = 0m;
        DateTime nextRenewalUtc;

        if (isAnnual)
        {
            var fullYearAmount = monthlyPrice * 12m;
            var discountFactor = (100m - discountPct) / 100m;
            totalPayableNow = Math.Round(fullYearAmount * discountFactor, 2);
            savingsAmount = Math.Round(fullYearAmount - totalPayableNow, 2);
            nextRenewalUtc = DateTime.UtcNow.AddDays(365);
        }
        else
        {
            totalPayableNow = monthlyPrice;
            nextRenewalUtc = DateTime.UtcNow.AddDays(30);
        }

        var preview = new CheckoutPreviewDto(
            Tier: request.Tier,
            PlanName: planName,
            BillingCycle: isAnnual ? "Annual" : "Monthly",
            BaseMonthlyPrice: monthlyPrice,
            DiscountPercentage: discountPct,
            TotalPayableNow: totalPayableNow,
            SavingsAmount: savingsAmount,
            NextRenewalDateUtc: nextRenewalUtc);

        return Result<CheckoutPreviewDto>.Success(preview);
    }

    private static decimal GetDefaultMonthlyPrice(SubscriptionTier tier) => tier switch
    {
        SubscriptionTier.Starter => 197.00m,
        SubscriptionTier.Pro => 497.00m,
        SubscriptionTier.Enterprise => 1497.00m,
        _ => 0m
    };
}
