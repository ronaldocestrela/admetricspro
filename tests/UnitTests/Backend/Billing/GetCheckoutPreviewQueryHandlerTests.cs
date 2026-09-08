using FluentAssertions;
using Master.Application.Billing.Checkout.Queries.GetCheckoutPreview;
using Master.Application.Repositories;
using Master.Domain.Plans;
using Master.Domain.Tenants;
using NSubstitute;

namespace UnitTests.Backend.Billing;

/// <summary>
/// Testes unitários para a consulta de pré-visualização de checkout (<see cref="GetCheckoutPreviewQueryHandler"/>).
/// </summary>
public sealed class GetCheckoutPreviewQueryHandlerTests
{
    private readonly IPlanRepository _planRepository = Substitute.For<IPlanRepository>();
    private readonly ITenantRepository _tenantRepository = Substitute.For<ITenantRepository>();

    /// <summary>
    /// Valida cálculo de valor para ciclo mensal sem aplicação de desconto anual.
    /// </summary>
    [Fact]
    public async Task Handle_WithMonthlyCycle_ShouldCalculateMonthlyPriceWithoutDiscount()
    {
        // Arrange
        var tenant = Tenant.Create("Agência Pro", "12345678000195", "agencia-pro").Value;
        _tenantRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>())
            .Returns(tenant);

        var plan = SubscriptionPlan.Create(
            "Plano Pro",
            "Para agências em expansão",
            SubscriptionTier.Pro,
            monthlyPrice: 497.00m,
            annualDiscountPercentage: 20,
            PlanLimits.Create(15, 10, 250_000m).Value,
            PlanFeatures.Default()).Value;

        _planRepository.GetByTierAsync(SubscriptionTier.Pro, Arg.Any<CancellationToken>())
            .Returns(plan);

        var handler = new GetCheckoutPreviewQueryHandler(_tenantRepository, _planRepository);
        var query = new GetCheckoutPreviewQuery(tenant.Id.Value, SubscriptionTier.Pro, "Monthly");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var preview = result.Value;
        preview.Tier.Should().Be(SubscriptionTier.Pro);
        preview.PlanName.Should().Be("Plano Pro");
        preview.BillingCycle.Should().Be("Monthly");
        preview.BaseMonthlyPrice.Should().Be(497.00m);
        preview.DiscountPercentage.Should().Be(0);
        preview.TotalPayableNow.Should().Be(497.00m);
        preview.SavingsAmount.Should().Be(0m);
    }

    /// <summary>
    /// Valida cálculo de valor para ciclo anual aplicando desconto contratual.
    /// </summary>
    [Fact]
    public async Task Handle_WithAnnualCycle_ShouldApplyAnnualDiscountPercentage()
    {
        // Arrange
        var tenant = Tenant.Create("Agência Pro", "12345678000195", "agencia-pro").Value;
        _tenantRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>())
            .Returns(tenant);

        var plan = SubscriptionPlan.Create(
            "Plano Pro",
            "Para agências em expansão",
            SubscriptionTier.Pro,
            monthlyPrice: 497.00m,
            annualDiscountPercentage: 20,
            PlanLimits.Create(15, 10, 250_000m).Value,
            PlanFeatures.Default()).Value;

        _planRepository.GetByTierAsync(SubscriptionTier.Pro, Arg.Any<CancellationToken>())
            .Returns(plan);

        var handler = new GetCheckoutPreviewQueryHandler(_tenantRepository, _planRepository);
        var query = new GetCheckoutPreviewQuery(tenant.Id.Value, SubscriptionTier.Pro, "Annual");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var preview = result.Value;
        preview.BillingCycle.Should().Be("Annual");
        preview.DiscountPercentage.Should().Be(20);
        // Preço cheio: 497 * 12 = 5964. Com 20% off: 5964 * 0.8 = 4771.20. Economia: 1192.80
        preview.TotalPayableNow.Should().Be(4771.20m);
        preview.SavingsAmount.Should().Be(1192.80m);
    }

    /// <summary>
    /// Valida que tenant inexistente retorna erro de não encontrado.
    /// </summary>
    [Fact]
    public async Task Handle_WhenTenantNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        _tenantRepository.GetByIdAsync(Arg.Any<TenantId>(), Arg.Any<CancellationToken>())
            .Returns((Tenant?)null);

        var handler = new GetCheckoutPreviewQueryHandler(_tenantRepository, _planRepository);
        var query = new GetCheckoutPreviewQuery(Guid.NewGuid(), SubscriptionTier.Pro, "Monthly");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.NotFound");
    }
}
