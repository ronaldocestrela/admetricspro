using BuildingBlocks.Application.Tenants.Queries.GetTenantPlanLimits;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Master.Application.Repositories;
using Master.Application.Tenants.Queries.GetTenantPlanLimits;
using Master.Domain.Plans;
using Master.Domain.Tenants;
using NSubstitute;
using Xunit;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Testes unitários para o manipulador de consulta <see cref="GetTenantPlanLimitsQueryHandler"/>.
/// </summary>
public sealed class GetTenantPlanLimitsQueryHandlerTests
{
    private readonly ITenantRepository _tenantRepository = Substitute.For<ITenantRepository>();
    private readonly IPlanRepository _planRepository = Substitute.For<IPlanRepository>();
    private readonly GetTenantPlanLimitsQueryHandler _handler;

    /// <summary>
    /// Inicializa a suíte de testes configurando mocks e o handler.
    /// </summary>
    public GetTenantPlanLimitsQueryHandlerTests()
    {
        _handler = new GetTenantPlanLimitsQueryHandler(_tenantRepository, _planRepository);
    }

    /// <summary>
    /// Valida que ao consultar limites de inquilino inexistente, retorna erro NotFound.
    /// </summary>
    [Fact]
    public async Task Handle_QuandoTenantNaoExiste_DeveRetornarNotFound()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _tenantRepository.GetByIdAsync(Arg.Any<TenantId>(), Arg.Any<CancellationToken>())
            .Returns((Tenant?)null);

        var query = new GetTenantPlanLimitsQuery(tenantId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.NotFound");
    }

    /// <summary>
    /// Valida que ao consultar limites de inquilino Pro com plano cadastrado, retorna as cotas do plano.
    /// </summary>
    [Fact]
    public async Task Handle_QuandoTenantPossuiPlanoNoCatalogo_DeveRetornarLimitesDoPlano()
    {
        // Arrange
        var tenant = Tenant.Create(
            "Agência Alfa",
            "12345678000195",
            "agencia-alfa",
            SubscriptionTier.Pro).Value;
        var tenantId = tenant.Id.Value;

        _tenantRepository.GetByIdAsync(Arg.Is<TenantId>(id => id.Value == tenantId), Arg.Any<CancellationToken>())
            .Returns(tenant);

        var planLimits = PlanLimits.Create(maxSeats: 10, maxWorkspaces: 15, monthlyAdSpendCap: 200000m).Value;
        var plan = SubscriptionPlan.Create(
            "Plano Pro",
            "Descrição Pro",
            SubscriptionTier.Pro,
            597m,
            20,
            planLimits,
            PlanFeatures.Default()).Value;

        _planRepository.GetByTierAsync(SubscriptionTier.Pro, Arg.Any<CancellationToken>())
            .Returns(plan);

        var query = new GetTenantPlanLimitsQuery(tenantId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Tier.Should().Be("Pro");
        result.Value.MaxWorkspaces.Should().Be(15);
        result.Value.MaxSeats.Should().Be(10);
        result.Value.MonthlyAdSpendCap.Should().Be(200000m);
    }

    /// <summary>
    /// Valida que ao consultar limites de inquilino Starter sem registro em SubscriptionPlans, usa fallback padrão do tier.
    /// </summary>
    [Fact]
    public async Task Handle_QuandoNaoExistePlanoNoCatalogo_DeveRetornarFallbackPadraoDoTier()
    {
        // Arrange
        var tenant = Tenant.Create(
            "Agência Starter",
            "12345678000195",
            "agencia-starter",
            SubscriptionTier.Starter).Value;
        var tenantId = tenant.Id.Value;

        _tenantRepository.GetByIdAsync(Arg.Is<TenantId>(id => id.Value == tenantId), Arg.Any<CancellationToken>())
            .Returns(tenant);

        _planRepository.GetByTierAsync(SubscriptionTier.Starter, Arg.Any<CancellationToken>())
            .Returns((SubscriptionPlan?)null);

        var query = new GetTenantPlanLimitsQuery(tenantId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Tier.Should().Be("Starter");
        result.Value.MaxWorkspaces.Should().Be(3);
    }
}
