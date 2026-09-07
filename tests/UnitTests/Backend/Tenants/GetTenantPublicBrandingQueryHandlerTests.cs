using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using NSubstitute;
using Tenants.Application.Auth.DTOs;
using Tenants.Application.Auth.Queries.GetTenantPublicBranding;
using Tenants.Application.Auth.Services;
using Xunit;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Testes unitários para o manipulador da consulta de branding público do inquilino (<see cref="GetTenantPublicBrandingQueryHandler"/>).
/// Valida cenários de sucesso, inquilino inexistente, inquilino inativo/suspenso e repasse ao serviço de autenticação.
/// </summary>
public sealed class GetTenantPublicBrandingQueryHandlerTests
{
    private readonly ITenantAuthService _tenantAuthService = Substitute.For<ITenantAuthService>();
    private readonly GetTenantPublicBrandingQueryHandler _handler;

    /// <summary>
    /// Inicializa a suíte de testes com instâncias simuladas.
    /// </summary>
    public GetTenantPublicBrandingQueryHandlerTests()
    {
        _handler = new GetTenantPublicBrandingQueryHandler(_tenantAuthService);
    }

    /// <summary>
    /// Valida que ao buscar um subdomínio válido e ativo, retorna os dados de branding com status 200/Sucesso.
    /// </summary>
    [Fact]
    public async Task Handle_WithValidAndActiveTenant_ShouldReturnBrandingSuccessfully()
    {
        // Arrange
        var expectedDto = new TenantPublicBrandingDto(
            TenantId: Guid.NewGuid(),
            CompanyName: "Agência Vanguarda",
            Subdomain: "vanguarda",
            CustomDomain: null,
            PrimaryColor: "#1E40AF",
            SecondaryColor: "#F59E0B",
            LogoUrl: null,
            IsActive: true);

        _tenantAuthService.GetPublicBrandingAsync("vanguarda", Arg.Any<CancellationToken>())
            .Returns(Result<TenantPublicBrandingDto>.Success(expectedDto));

        var query = new GetTenantPublicBrandingQuery("vanguarda");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedDto);
    }

    /// <summary>
    /// Valida que ao buscar um subdomínio não cadastrado, retorna erro NotFound repassado pelo serviço.
    /// </summary>
    [Fact]
    public async Task Handle_WhenTenantNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        _tenantAuthService.GetPublicBrandingAsync("inexistente", Arg.Any<CancellationToken>())
            .Returns(Result<TenantPublicBrandingDto>.Failure(
                Error.NotFound("Tenant.NotFound", "Inquilino não localizado.")));

        var query = new GetTenantPublicBrandingQuery("inexistente");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.NotFound");
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    /// <summary>
    /// Valida que inquilino suspenso ou inativo retorna erro de negócio Tenant.Inactive.
    /// </summary>
    [Fact]
    public async Task Handle_WhenTenantIsSuspended_ShouldReturnInactiveError()
    {
        // Arrange
        _tenantAuthService.GetPublicBrandingAsync("suspensa", Arg.Any<CancellationToken>())
            .Returns(Result<TenantPublicBrandingDto>.Failure(
                Error.Validation("Tenant.Inactive", "O acesso do inquilino está inativo ou suspenso.")));

        var query = new GetTenantPublicBrandingQuery("suspensa");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.Inactive");
        result.Error.Type.Should().Be(ErrorType.Validation);
    }
}
