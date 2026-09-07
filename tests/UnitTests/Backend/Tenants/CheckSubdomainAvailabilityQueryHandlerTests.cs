using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Master.Application.Repositories;
using Master.Application.Tenants.Queries.CheckSubdomainAvailability;
using Master.Domain.Tenants;
using NSubstitute;
using Xunit;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Testes unitários para o handler de verificação de disponibilidade de subdomínio (<see cref="CheckSubdomainAvailabilityQueryHandler"/>).
/// </summary>
public sealed class CheckSubdomainAvailabilityQueryHandlerTests
{
    private readonly ITenantRepository _tenantRepository = Substitute.For<ITenantRepository>();

    /// <summary>
    /// Valida que um subdomínio válido e inexistente no catálogo é reportado como disponível.
    /// </summary>
    [Fact]
    public async Task Handle_WhenSubdomainIsAvailable_ShouldReturnSuccessWithAvailableTrue()
    {
        // Arrange
        _tenantRepository.GetBySubdomainAsync("vanguarda", Arg.Any<CancellationToken>())
            .Returns((Tenant?)null);

        var handler = new CheckSubdomainAvailabilityQueryHandler(_tenantRepository);
        var query = new CheckSubdomainAvailabilityQuery("vanguarda");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsAvailable.Should().BeTrue();
        result.Value.Subdomain.Should().Be("vanguarda");
        result.Value.Reason.Should().BeNull();
    }

    /// <summary>
    /// Valida que um subdomínio reservado do sistema (ex: 'admin', 'api', 'master') é rejeitado como indisponível.
    /// </summary>
    [Theory]
    [InlineData("admin")]
    [InlineData("api")]
    [InlineData("master")]
    [InlineData("app")]
    [InlineData("auth")]
    public async Task Handle_WhenSubdomainIsReserved_ShouldReturnSuccessWithAvailableFalse(string reservedSubdomain)
    {
        // Arrange
        var handler = new CheckSubdomainAvailabilityQueryHandler(_tenantRepository);
        var query = new CheckSubdomainAvailabilityQuery(reservedSubdomain);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsAvailable.Should().BeFalse();
        result.Value.Reason.Should().Contain("reservado");
    }

    /// <summary>
    /// Valida que um subdomínio que já pertence a outro tenant é reportado como indisponível e sugere alternativa.
    /// </summary>
    [Fact]
    public async Task Handle_WhenSubdomainAlreadyExists_ShouldReturnSuccessWithAvailableFalseAndSuggestion()
    {
        // Arrange
        var existingTenant = Tenant.Create("Empresa Existente", "12345678000195", "vanguarda").Value;
        _tenantRepository.GetBySubdomainAsync("vanguarda", Arg.Any<CancellationToken>())
            .Returns(existingTenant);

        var handler = new CheckSubdomainAvailabilityQueryHandler(_tenantRepository);
        var query = new CheckSubdomainAvailabilityQuery("vanguarda");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsAvailable.Should().BeFalse();
        result.Value.Reason.Should().Contain("já está em uso");
        result.Value.SuggestedAlternative.Should().NotBeNullOrWhiteSpace();
    }
}
