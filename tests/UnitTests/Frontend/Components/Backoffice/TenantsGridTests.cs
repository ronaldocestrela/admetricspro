using AngleSharp.Dom;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using UnitTests.Frontend.Common;
using WebApp.Components.Backoffice;
using WebApp.Models;

namespace UnitTests.Frontend.Components.Backoffice;

/// <summary>
/// Testes de componente bUnit para a tabela interativa de listagem de inquilinos (<see cref="TenantsGrid"/>).
/// Valida a renderização de linhas, busca textual reativa, filtros por status/tier e emissão de callbacks.
/// </summary>
public class TenantsGridTests : BunitTestBase
{
    private static readonly List<TenantDirectoryItemViewModel> SampleTenants =
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
            SunkAdSpend: 5000m),
        new(
            Id: Guid.Parse("33333333-3333-3333-3333-333333333333"),
            CompanyName: "Gamma Growth Lab",
            Cnpj: "33444555000103",
            Subdomain: "gammalab",
            Status: "Trial",
            Tier: "Trial",
            SubscriptionExpiresAtUtc: DateTime.UtcNow.AddDays(7),
            CreatedAtUtc: DateTime.UtcNow.AddDays(-7),
            WorkspacesCount: 2,
            SunkAdSpend: 1200m)
    ];

    /// <summary>
    /// Valida que a tabela renderiza todas as linhas de inquilinos fornecidas com suas informações formatadas.
    /// </summary>
    [Fact]
    public void TenantsGrid_WhenLoadedWithTenants_ShouldRenderTableRows()
    {
        // Act
        var cut = Render<TenantsGrid>(parameters => parameters
            .Add(p => p.Tenants, SampleTenants)
            .Add(p => p.IsLoading, false));

        // Assert
        var rows = cut.FindAll("tbody tr.tenant-row");
        rows.Should().HaveCount(3);

        cut.Markup.Should().Contain("Alpha Digital Tech");
        cut.Markup.Should().Contain("11.222.333/0001-81");
        cut.Markup.Should().Contain("alphatech");
        cut.Markup.Should().Contain("Beta Marketing Global");
        cut.Markup.Should().Contain("Gamma Growth Lab");
    }

    /// <summary>
    /// Valida que quando IsLoading é verdadeiro, um indicador de carregamento é exibido e as linhas não são mostradas.
    /// </summary>
    [Fact]
    public void TenantsGrid_WhenIsLoadingIsTrue_ShouldRenderLoadingIndicator()
    {
        // Act
        var cut = Render<TenantsGrid>(parameters => parameters
            .Add(p => p.Tenants, SampleTenants)
            .Add(p => p.IsLoading, true));

        // Assert
        cut.Find(".grid-loading-indicator").Should().NotBeNull();
        cut.FindAll("tbody tr.tenant-row").Should().BeEmpty();
    }

    /// <summary>
    /// Valida que a digitação no campo de pesquisa textual filtra dinamicamente as linhas correspondentes por nome ou subdomínio.
    /// </summary>
    [Fact]
    public void TenantsGrid_WhenSearchTermEntered_ShouldFilterRows()
    {
        // Arrange
        var cut = Render<TenantsGrid>(parameters => parameters
            .Add(p => p.Tenants, SampleTenants)
            .Add(p => p.IsLoading, false));

        var searchInput = cut.Find("input.search-input");

        // Act - filtra por termo existente
        searchInput.Input("Alpha");

        // Assert
        var rows = cut.FindAll("tbody tr.tenant-row");
        rows.Should().HaveCount(1);
        cut.Markup.Should().Contain("Alpha Digital Tech");
        cut.Markup.Should().NotContain("Beta Marketing Global");
    }

    /// <summary>
    /// Valida que a seleção de um filtro por status exibe apenas os tenants com o status selecionado.
    /// </summary>
    [Fact]
    public void TenantsGrid_WhenStatusFilterChanged_ShouldFilterRows()
    {
        // Arrange
        var cut = Render<TenantsGrid>(parameters => parameters
            .Add(p => p.Tenants, SampleTenants)
            .Add(p => p.IsLoading, false));

        var statusSelect = cut.Find("select.status-filter");

        // Act - seleciona status "Suspended"
        statusSelect.Change("Suspended");

        // Assert
        var rows = cut.FindAll("tbody tr.tenant-row");
        rows.Should().HaveCount(1);
        cut.Markup.Should().Contain("Beta Marketing Global");
        cut.Markup.Should().NotContain("Alpha Digital Tech");
    }

    /// <summary>
    /// Valida que quando a busca não encontra nenhum inquilino, o componente exibe uma mensagem de estado vazio.
    /// </summary>
    [Fact]
    public void TenantsGrid_WhenNoMatchingResults_ShouldRenderEmptyState()
    {
        // Arrange
        var cut = Render<TenantsGrid>(parameters => parameters
            .Add(p => p.Tenants, SampleTenants)
            .Add(p => p.IsLoading, false));

        var searchInput = cut.Find("input.search-input");

        // Act - busca termo inexistente
        searchInput.Input("Inexistente12345");

        // Assert
        cut.Find(".grid-empty-state").Should().NotBeNull();
        cut.Find(".grid-empty-state").TextContent.Should().Contain("Nenhum inquilino encontrado");
    }

    /// <summary>
    /// Valida que clicar no botão de detalhes 360º de um tenant dispara o evento OnSelectTenant com o item selecionado.
    /// </summary>
    [Fact]
    public void TenantsGrid_WhenViewDetailsClicked_ShouldTriggerOnSelectTenantCallback()
    {
        // Arrange
        TenantDirectoryItemViewModel? selectedTenant = null;

        var cut = Render<TenantsGrid>(parameters => parameters
            .Add(p => p.Tenants, SampleTenants)
            .Add(p => p.IsLoading, false)
            .Add(p => p.OnSelectTenant, EventCallback.Factory.Create<TenantDirectoryItemViewModel>(this, t => selectedTenant = t)));

        // Act - clica no botão de detalhes do primeiro tenant
        var detailsButton = cut.FindAll("button.btn-view-details").First();
        detailsButton.Click();

        // Assert
        selectedTenant.Should().NotBeNull();
        selectedTenant!.CompanyName.Should().Be("Alpha Digital Tech");
    }

    /// <summary>
    /// Valida que clicar no botão de suspender de um tenant ativo dispara o evento OnSuspendTenant com o item correspondente.
    /// </summary>
    [Fact]
    public void TenantsGrid_WhenSuspendClicked_ShouldTriggerOnSuspendTenantCallback()
    {
        // Arrange
        TenantDirectoryItemViewModel? suspendedTenant = null;

        var cut = Render<TenantsGrid>(parameters => parameters
            .Add(p => p.Tenants, SampleTenants)
            .Add(p => p.IsLoading, false)
            .Add(p => p.OnSuspendTenant, EventCallback.Factory.Create<TenantDirectoryItemViewModel>(this, t => suspendedTenant = t)));

        // Act - clica no botão de suspender do tenant ativo (Alpha)
        var suspendButton = cut.Find("button.btn-suspend-tenant");
        suspendButton.Click();

        // Assert
        suspendedTenant.Should().NotBeNull();
        suspendedTenant!.CompanyName.Should().Be("Alpha Digital Tech");
    }

    /// <summary>
    /// Valida que clicar no botão de reativar de um tenant suspenso dispara o evento OnReactivateTenant com o item correspondente.
    /// </summary>
    [Fact]
    public void TenantsGrid_WhenReactivateClicked_ShouldTriggerOnReactivateTenantCallback()
    {
        // Arrange
        TenantDirectoryItemViewModel? reactivatedTenant = null;

        var cut = Render<TenantsGrid>(parameters => parameters
            .Add(p => p.Tenants, SampleTenants)
            .Add(p => p.IsLoading, false)
            .Add(p => p.OnReactivateTenant, EventCallback.Factory.Create<TenantDirectoryItemViewModel>(this, t => reactivatedTenant = t)));

        // Act - clica no botão de reativar do tenant suspenso (Beta)
        var reactivateButton = cut.Find("button.btn-reactivate-tenant");
        reactivateButton.Click();

        // Assert
        reactivatedTenant.Should().NotBeNull();
        reactivatedTenant!.CompanyName.Should().Be("Beta Marketing Global");
    }
}
