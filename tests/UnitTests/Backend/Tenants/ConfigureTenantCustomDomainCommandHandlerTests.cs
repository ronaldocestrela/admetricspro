using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Master.Application.Repositories;
using Master.Application.Tenants.Commands.ConfigureTenantCustomDomain;
using Master.Domain.Plans;
using Master.Domain.Tenants;
using NSubstitute;
using Xunit;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Unit tests for <see cref="ConfigureTenantCustomDomainCommandHandler"/>.
/// </summary>
public sealed class ConfigureTenantCustomDomainCommandHandlerTests
{
    private readonly ITenantRepository _tenantRepository = Substitute.For<ITenantRepository>();
    private readonly IPlanRepository _planRepository = Substitute.For<IPlanRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ConfigureTenantCustomDomainCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    /// <summary>
    /// Initializes a new instance of <see cref="ConfigureTenantCustomDomainCommandHandlerTests"/>.
    /// </summary>
    public ConfigureTenantCustomDomainCommandHandlerTests()
    {
        _handler = new ConfigureTenantCustomDomainCommandHandler(
            _tenantRepository,
            _planRepository,
            _unitOfWork);
    }

    private Tenant CreateTestTenant(bool withCnameFeature = true)
    {
        var tenant = Tenant.Create(
            companyName: "Agência Alfa",
            cnpj: "12345678000195",
            subdomain: "agencia-alfa",
            tier: SubscriptionTier.Enterprise).Value;

        var features = PlanFeatures.Create(
            hasWhiteLabel: withCnameFeature,
            hasCustomCname: withCnameFeature,
            hasAiCopilot: false,
            hasCrossNetworkAutomations: false).Value;

        var plan = SubscriptionPlan.Create(
            "Plano Pro",
            "Plano com White-Label e CNAME",
            SubscriptionTier.Enterprise,
            999m,
            10,
            PlanLimits.Create(50, 10, 500000m).Value,
            features).Value;

        _planRepository.GetByTierAsync(SubscriptionTier.Enterprise, Arg.Any<CancellationToken>()).Returns(plan);
        _tenantRepository.GetByIdAsync(Arg.Any<TenantId>(), Arg.Any<CancellationToken>()).Returns(tenant);

        return tenant;
    }

    /// <summary>
    /// Verifies that configuring a valid CNAME domain succeeds when plan supports it.
    /// </summary>
    [Fact]
    public async Task Handle_WithValidCustomDomainAndPlan_ShouldSucceedAndCommit()
    {
        // Arrange
        CreateTestTenant(withCnameFeature: true);
        _tenantRepository.GetByCustomDomainAsync("relatorios.agenciaalfa.com.br", Arg.Any<CancellationToken>())
            .Returns((Tenant?)null);

        var command = new ConfigureTenantCustomDomainCommand(_tenantId, "relatorios.agenciaalfa.com.br");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that when the plan does not support CNAME, Forbidden error is returned.
    /// </summary>
    [Fact]
    public async Task Handle_WhenPlanDoesNotSupportCname_ShouldReturnForbidden()
    {
        // Arrange
        CreateTestTenant(withCnameFeature: false);

        var command = new ConfigureTenantCustomDomainCommand(_tenantId, "relatorios.agenciaalfa.com.br");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.PlanLacksCustomCname");
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that configuring a CNAME with protocol or invalid characters returns validation failure.
    /// </summary>
    [Theory]
    [InlineData("https://relatorios.agencia.com")]
    [InlineData("http://relatorios.agencia.com")]
    [InlineData("relatorios.agencia.com:8080")]
    [InlineData("relatorios.agencia.com/path")]
    [InlineData("singleword")]
    public async Task Handle_WithInvalidDomainFormat_ShouldReturnValidationFailure(string invalidDomain)
    {
        // Arrange
        CreateTestTenant(withCnameFeature: true);

        var command = new ConfigureTenantCustomDomainCommand(_tenantId, invalidDomain);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.InvalidCustomDomain");
    }

    /// <summary>
    /// Verifies that when custom domain is already registered by another tenant, Conflict is returned.
    /// </summary>
    [Fact]
    public async Task Handle_WhenDomainAlreadyInUse_ShouldReturnConflict()
    {
        // Arrange
        CreateTestTenant(withCnameFeature: true);
        var otherTenant = Tenant.Create(
            companyName: "Outra Agência",
            cnpj: "98765432000199",
            subdomain: "outra-agencia",
            tier: SubscriptionTier.Enterprise,
            customDomain: "relatorios.agenciaalfa.com.br").Value;

        _tenantRepository.GetByCustomDomainAsync("relatorios.agenciaalfa.com.br", Arg.Any<CancellationToken>())
            .Returns(otherTenant);

        var command = new ConfigureTenantCustomDomainCommand(_tenantId, "relatorios.agenciaalfa.com.br");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.CustomDomainConflict");
    }
}
