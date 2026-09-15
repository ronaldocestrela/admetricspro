using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Master.Application.Repositories;
using Master.Application.Tenants.Queries.GetTenantCustomDomain;
using Master.Domain.Plans;
using Master.Domain.Tenants;
using NSubstitute;
using Xunit;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Unit tests for <see cref="GetTenantCustomDomainQueryHandler"/>.
/// </summary>
public sealed class GetTenantCustomDomainQueryHandlerTests
{
    private readonly ITenantRepository _tenantRepository = Substitute.For<ITenantRepository>();
    private readonly IPlanRepository _planRepository = Substitute.For<IPlanRepository>();
    private readonly GetTenantCustomDomainQueryHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    /// <summary>
    /// Initializes a new instance of <see cref="GetTenantCustomDomainQueryHandlerTests"/>.
    /// </summary>
    public GetTenantCustomDomainQueryHandlerTests()
    {
        _handler = new GetTenantCustomDomainQueryHandler(_tenantRepository, _planRepository);
    }

    /// <summary>
    /// Verifies that when tenant is not found, NotFound is returned.
    /// </summary>
    [Fact]
    public async Task Handle_WhenTenantDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        _tenantRepository.GetByIdAsync(Arg.Any<TenantId>(), Arg.Any<CancellationToken>())
            .Returns((Tenant?)null);

        var query = new GetTenantCustomDomainQuery(_tenantId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.NotFound");
    }

    /// <summary>
    /// Verifies that when tenant exists with custom domain, configured status and expected CNAME target are returned.
    /// </summary>
    [Fact]
    public async Task Handle_WhenTenantExistsWithCustomDomain_ShouldReturnDtoWithDomainAndExpectedCname()
    {
        // Arrange
        var tenant = Tenant.Create(
            companyName: "Agência Alfa",
            cnpj: "12345678000195",
            subdomain: "agencia-alfa",
            tier: SubscriptionTier.Enterprise,
            customDomain: "relatorios.agenciaalfa.com.br").Value;

        var features = PlanFeatures.Create(
            hasWhiteLabel: true,
            hasCustomCname: true,
            hasAiCopilot: false,
            hasCrossNetworkAutomations: false).Value;

        var plan = SubscriptionPlan.Create(
            "Enterprise",
            "Enterprise Plan",
            SubscriptionTier.Enterprise,
            999m,
            10,
            PlanLimits.Create(50, 10, 500000m).Value,
            features).Value;

        _tenantRepository.GetByIdAsync(Arg.Any<TenantId>(), Arg.Any<CancellationToken>())
            .Returns(tenant);
        _planRepository.GetByTierAsync(SubscriptionTier.Enterprise, Arg.Any<CancellationToken>())
            .Returns(plan);

        var query = new GetTenantCustomDomainQuery(_tenantId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CustomDomain.Should().Be("relatorios.agenciaalfa.com.br");
        result.Value.IsConfigured.Should().BeTrue();
        result.Value.HasPlanSupport.Should().BeTrue();
        result.Value.ExpectedCnameTarget.Should().Be("cname.admetricspro.com");
    }
}
