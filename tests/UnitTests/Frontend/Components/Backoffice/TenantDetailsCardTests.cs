using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using UnitTests.Frontend.Common;
using WebApp.Components.Backoffice;
using WebApp.Models;

namespace UnitTests.Frontend.Components.Backoffice;

/// <summary>
/// Testes de componente bUnit para a Ficha 360º do Inquilino (<see cref="TenantDetailsCard"/>).
/// Valida a renderização dos dados cadastrais/fiscais, contratuais, métricas operacionais consolidadas e botões de ação rápida.
/// </summary>
public class TenantDetailsCardTests : BunitTestBase
{
    private static readonly Tenant360DetailsViewModel ActiveTenant = new(
        Id: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        CompanyName: "Acme Corporation Ltda",
        Cnpj: "12345678000190",
        Subdomain: "acme",
        CustomDomain: "ads.acme.com.br",
        Status: "Active",
        Tier: "Enterprise",
        SubscriptionExpiresAtUtc: new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc),
        CreatedAtUtc: new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc),
        WorkspacesCount: 5,
        SunkAdSpend: 125000.50m,
        ActiveIntegrationsCount: 4,
        TotalCampaignsCount: 22);

    private static readonly Tenant360DetailsViewModel SuspendedTenant = new(
        Id: Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
        CompanyName: "Inativo Digital S/A",
        Cnpj: "98765432000109",
        Subdomain: "inativodigital",
        CustomDomain: null,
        Status: "Suspended",
        Tier: "Starter",
        SubscriptionExpiresAtUtc: new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
        CreatedAtUtc: new DateTime(2025, 6, 1, 10, 0, 0, DateTimeKind.Utc),
        WorkspacesCount: 1,
        SunkAdSpend: 3200.00m,
        ActiveIntegrationsCount: 1,
        TotalCampaignsCount: 2);

    /// <summary>
    /// Valida que quando o parâmetro Tenant for nulo, o componente não renderiza o card detalhado.
    /// </summary>
    [Fact]
    public void TenantDetailsCard_WhenTenantIsNull_ShouldNotRenderCard()
    {
        // Act
        var cut = Render<TenantDetailsCard>(parameters => parameters
            .Add(p => p.Tenant, null));

        // Assert
        cut.FindAll(".tenant-details-card").Should().BeEmpty();
    }

    /// <summary>
    /// Valida a renderização precisa de todos os dados fiscais e contratuais da empresa.
    /// </summary>
    [Fact]
    public void TenantDetailsCard_WhenTenantProvided_ShouldRenderFiscalAndContractData()
    {
        // Act
        var cut = Render<TenantDetailsCard>(parameters => parameters
            .Add(p => p.Tenant, ActiveTenant));

        // Assert
        cut.Find(".tenant-details-card").Should().NotBeNull();
        cut.Markup.Should().Contain("Acme Corporation Ltda");
        cut.Markup.Should().Contain("12.345.678/0001-90");
        cut.Markup.Should().Contain("acme");
        cut.Markup.Should().Contain("ads.acme.com.br");
        cut.Markup.Should().Contain("Enterprise");
        cut.Markup.Should().Contain("Ativo");
    }

    /// <summary>
    /// Valida que as métricas operacionais 360º (workspaces, ad spend, integradores e campanhas) são exibidas corretamente.
    /// </summary>
    [Fact]
    public void TenantDetailsCard_WhenTenantProvided_ShouldRenderOperational360Metrics()
    {
        // Act
        var cut = Render<TenantDetailsCard>(parameters => parameters
            .Add(p => p.Tenant, ActiveTenant));

        // Assert
        var metricsSection = cut.Find(".metrics-360-grid");
        metricsSection.Should().NotBeNull();

        cut.Markup.Should().Contain("5"); // Workspaces
        cut.Markup.Should().Contain("125.000,50"); // Ad spend formatado
        cut.Markup.Should().Contain("4"); // Integrações ativas
        cut.Markup.Should().Contain("22"); // Campanhas totais
    }

    /// <summary>
    /// Valida que um tenant ativo exibe o botão de suspensão e não exibe o botão de reativação.
    /// Ao clicar no botão de suspensão, emite o callback OnSuspendClick com o identificador do tenant.
    /// </summary>
    [Fact]
    public void TenantDetailsCard_WhenTenantIsActive_ShouldRenderSuspendButtonAndEmitCallback()
    {
        // Arrange
        Guid? suspendedTenantId = null;

        var cut = Render<TenantDetailsCard>(parameters => parameters
            .Add(p => p.Tenant, ActiveTenant)
            .Add(p => p.OnSuspendClick, EventCallback.Factory.Create<Guid>(this, id => suspendedTenantId = id)));

        // Assert botões
        cut.FindAll("button.btn-card-suspend").Should().HaveCount(1);
        cut.FindAll("button.btn-card-reactivate").Should().BeEmpty();

        // Act
        cut.Find("button.btn-card-suspend").Click();

        // Assert callback
        suspendedTenantId.Should().Be(ActiveTenant.Id);
    }

    /// <summary>
    /// Valida que um tenant suspenso exibe o botão de reativação e não exibe o de suspensão.
    /// Ao clicar em reativar, emite o callback OnReactivateClick com o identificador do tenant.
    /// </summary>
    [Fact]
    public void TenantDetailsCard_WhenTenantIsSuspended_ShouldRenderReactivateButtonAndEmitCallback()
    {
        // Arrange
        Guid? reactivatedTenantId = null;

        var cut = Render<TenantDetailsCard>(parameters => parameters
            .Add(p => p.Tenant, SuspendedTenant)
            .Add(p => p.OnReactivateClick, EventCallback.Factory.Create<Guid>(this, id => reactivatedTenantId = id)));

        // Assert botões
        cut.FindAll("button.btn-card-reactivate").Should().HaveCount(1);
        cut.FindAll("button.btn-card-suspend").Should().BeEmpty();

        // Act
        cut.Find("button.btn-card-reactivate").Click();

        // Assert callback
        reactivatedTenantId.Should().Be(SuspendedTenant.Id);
    }

    /// <summary>
    /// Valida que clicar no botão de fechar emite o callback OnClose.
    /// </summary>
    [Fact]
    public void TenantDetailsCard_WhenCloseClicked_ShouldTriggerOnCloseCallback()
    {
        // Arrange
        var wasClosed = false;

        var cut = Render<TenantDetailsCard>(parameters => parameters
            .Add(p => p.Tenant, ActiveTenant)
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => wasClosed = true)));

        // Act
        cut.Find("button.btn-card-close").Click();

        // Assert
        wasClosed.Should().BeTrue();
    }
}
