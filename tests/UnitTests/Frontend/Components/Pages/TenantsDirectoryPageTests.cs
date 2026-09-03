using BuildingBlocks.Domain.Primitives;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using UnitTests.Frontend.Common;
using WebApp.Components.Pages.Admin;
using WebApp.Models;
using WebApp.Services;

namespace UnitTests.Frontend.Components.Pages;

/// <summary>
/// Testes de componente bUnit para a página do Diretório 360º de Inquilinos (<see cref="TenantsDirectoryPage"/>).
/// Valida a orquestração do carregamento assíncrono via <see cref="ITenantDirectoryService"/>, exibição de KPIs,
/// abertura da ficha 360º e execução de suspensão forçada com dupla confirmação.
/// </summary>
public class TenantsDirectoryPageTests : BunitTestBase
{
    private readonly ITenantDirectoryService _tenantService;

    private static readonly List<TenantDirectoryItemViewModel> MockTenants =
    [
        new(
            Id: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            CompanyName: "Alpha Digital Tech",
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
            CompanyName: "Beta Marketing Global",
            Cnpj: "22333444000192",
            Subdomain: "betamkt",
            Status: "Suspended",
            Tier: "Starter",
            SubscriptionExpiresAtUtc: DateTime.UtcNow.AddMonths(-1),
            CreatedAtUtc: DateTime.UtcNow.AddMonths(-3),
            WorkspacesCount: 1,
            SunkAdSpend: 5000m)
    ];

    public TenantsDirectoryPageTests()
    {
        _tenantService = Substitute.For<ITenantDirectoryService>();
        _tenantService.GetTenantsAsync(Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<TenantDirectoryItemViewModel>>.Success(MockTenants));

        Services.AddSingleton(_tenantService);
    }

    /// <summary>
    /// Valida que a página inicializa carregando a lista de inquilinos e renderizando os cards de KPI de governança.
    /// </summary>
    [Fact]
    public void TenantsDirectoryPage_OnInitialized_ShouldLoadTenantsAndRenderKpis()
    {
        // Act
        var cut = Render<TenantsDirectoryPage>();

        // Assert
        cut.Find(".page-title").TextContent.Should().Contain("Diretório 360º de Inquilinos");

        var kpiCards = cut.FindAll(".kpi-card");
        kpiCards.Should().HaveCount(4);

        cut.Markup.Should().Contain("Alpha Digital Tech");
        cut.Markup.Should().Contain("Beta Marketing Global");
    }

    /// <summary>
    /// Valida que ao acionar a seleção de um tenant, a ficha 360º é carregada e exibida em tela.
    /// </summary>
    [Fact]
    public void TenantsDirectoryPage_WhenTenantSelected_ShouldLoadAndDisplay360Details()
    {
        // Arrange
        var targetId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var details360 = new Tenant360DetailsViewModel(
            Id: targetId,
            CompanyName: "Alpha Digital Tech",
            Cnpj: "11222333000181",
            Subdomain: "alphatech",
            CustomDomain: "ads.alphatech.com.br",
            Status: "Active",
            Tier: "Pro",
            SubscriptionExpiresAtUtc: DateTime.UtcNow.AddMonths(6),
            CreatedAtUtc: DateTime.UtcNow.AddYears(-1),
            WorkspacesCount: 3,
            SunkAdSpend: 45000m,
            ActiveIntegrationsCount: 2,
            TotalCampaignsCount: 14);

        _tenantService.GetTenant360DetailsAsync(targetId, Arg.Any<CancellationToken>())
            .Returns(Result<Tenant360DetailsViewModel>.Success(details360));

        var cut = Render<TenantsDirectoryPage>();

        // Act - clica em ver detalhes do primeiro tenant
        var viewDetailsBtn = cut.FindAll("button.btn-view-details").First();
        viewDetailsBtn.Click();

        // Assert
        cut.Find(".tenant-details-card").Should().NotBeNull();
        cut.Markup.Should().Contain("ads.alphatech.com.br");
        cut.Markup.Should().Contain("Métricas Operacionais 360º");
    }

    /// <summary>
    /// Valida que o fluxo de suspensão forçada abre o modal de validação dupla e, após confirmação, chama o serviço.
    /// </summary>
    [Fact]
    public void TenantsDirectoryPage_WhenSuspendConfirmed_ShouldCallServiceAndRefresh()
    {
        // Arrange
        var targetId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        _tenantService.SuspendTenantAsync(targetId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var cut = Render<TenantsDirectoryPage>();

        // Act 1 - clica em suspender na listagem
        var suspendBtn = cut.Find("button.btn-suspend-tenant");
        suspendBtn.Click();

        // Assert modal aberto
        cut.Find(".confirm-action-dialog").Should().NotBeNull();

        // Act 2 - preenche os campos do diálogo
        cut.Find("textarea.reason-input").Input("Inadimplência financeira confirmada.");
        cut.Find("input.confirmation-text-input").Input("alphatech");

        // Act 3 - confirma ação
        cut.Find("button.btn-dialog-confirm").Click();

        // Assert chamada ao serviço
        _tenantService.Received(1).SuspendTenantAsync(
            targetId,
            "Inadimplência financeira confirmada.",
            Arg.Any<CancellationToken>());
    }
}
