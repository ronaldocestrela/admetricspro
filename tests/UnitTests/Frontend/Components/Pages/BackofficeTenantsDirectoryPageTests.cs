using BackofficeApp.Components.Pages;
using BackofficeApp.Models;
using BackofficeApp.Services;
using BuildingBlocks.Domain.Primitives;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using UnitTests.Frontend.Common;
using Xunit;

namespace UnitTests.Frontend.Components.Pages;

/// <summary>
/// Testes de componente bUnit para a página do Diretório 360º de Tenants do Backoffice (<see cref="TenantsDirectoryPage"/>).
/// Valida o ciclo de vida completo: listagem, visualização de ficha 360º e suspensão com confirmação.
/// </summary>
public sealed class BackofficeTenantsDirectoryPageTests : BunitTestBase
{
    private readonly ITenantDirectoryService _tenantService;

    private static readonly List<TenantDirectoryItemViewModel> MockTenants =
    [
        new(
            Id: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            CompanyName: "Alpha Tech Ltda",
            Cnpj: "11222333000181",
            Subdomain: "alphatech",
            Status: "Active",
            Tier: "Pro",
            SubscriptionExpiresAtUtc: DateTime.UtcNow.AddMonths(6),
            CreatedAtUtc: DateTime.UtcNow.AddYears(-1),
            WorkspacesCount: 3,
            SunkAdSpend: 45000m),
        new(
            Id: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            CompanyName: "Beta Marketing Corp",
            Cnpj: "22333444000192",
            Subdomain: "betamkt",
            Status: "Suspended",
            Tier: "Starter",
            SubscriptionExpiresAtUtc: DateTime.UtcNow.AddMonths(-1),
            CreatedAtUtc: DateTime.UtcNow.AddMonths(-3),
            WorkspacesCount: 1,
            SunkAdSpend: 5000m)
    ];

    public BackofficeTenantsDirectoryPageTests()
    {
        _tenantService = Substitute.For<ITenantDirectoryService>();
        _tenantService.GetTenantsAsync(Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<TenantDirectoryItemViewModel>>.Success(MockTenants));

        Services.AddSingleton(_tenantService);
    }

    /// <summary>
    /// Valida que a página renderiza sem falha de parâmetro desconhecido no TenantsGrid (OnTenantSelected vs OnSelectTenant).
    /// </summary>
    [Fact]
    public void TenantsDirectoryPage_OnInitialized_ShouldRenderWithoutParameterExceptions()
    {
        // Act
        var cut = Render<TenantsDirectoryPage>();

        // Assert
        cut.Find(".page-title").TextContent.Should().Contain("Diretório 360º de Inquilinos");
        cut.Markup.Should().Contain("Alpha Tech Ltda");
        cut.Markup.Should().Contain("Beta Marketing Corp");
    }

    /// <summary>
    /// Valida que ao selecionar um tenant, os detalhes 360º são carregados no card sem exceção de parâmetro 'Details'.
    /// </summary>
    [Fact]
    public void TenantsDirectoryPage_WhenTenantSelected_ShouldDisplay360DetailsCard()
    {
        // Arrange
        var targetId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var details = new Tenant360DetailsViewModel(
            Id: targetId,
            CompanyName: "Alpha Tech Ltda",
            Cnpj: "11222333000181",
            Subdomain: "alphatech",
            CustomDomain: "portal.alphatech.com",
            Status: "Active",
            Tier: "Pro",
            SubscriptionExpiresAtUtc: DateTime.UtcNow.AddMonths(6),
            CreatedAtUtc: DateTime.UtcNow.AddYears(-1),
            WorkspacesCount: 3,
            SunkAdSpend: 45000m,
            ActiveIntegrationsCount: 4,
            TotalCampaignsCount: 12);

        _tenantService.GetTenant360DetailsAsync(targetId, Arg.Any<CancellationToken>())
            .Returns(Result<Tenant360DetailsViewModel>.Success(details));

        var cut = Render<TenantsDirectoryPage>();

        // Act
        var viewDetailsBtn = cut.FindAll("button.btn-view-details").First();
        viewDetailsBtn.Click();

        // Assert
        cut.Find(".tenant-details-card").Should().NotBeNull();
        cut.Markup.Should().Contain("portal.alphatech.com");
    }

    /// <summary>
    /// Valida o fluxo de suspensão de inquilino via modal de confirmação.
    /// </summary>
    [Fact]
    public void TenantsDirectoryPage_WhenSuspendConfirmed_ShouldCallSuspendService()
    {
        // Arrange
        var targetId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        _tenantService.SuspendTenantAsync(targetId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var cut = Render<TenantsDirectoryPage>();

        // Act 1: Abrir modal
        var suspendBtn = cut.Find("button.btn-suspend-tenant");
        suspendBtn.Click();

        cut.Find(".confirm-action-dialog").Should().NotBeNull();

        // Act 2: Preencher e confirmar
        cut.Find("textarea.reason-input").Input("Suspensão judicial determinada");
        cut.Find("input.confirmation-text-input").Input("alphatech");
        cut.Find("button.btn-dialog-confirm").Click();

        // Assert
        _tenantService.Received(1).SuspendTenantAsync(
            targetId,
            "Suspensão judicial determinada",
            Arg.Any<CancellationToken>());
    }
}
