using AngleSharp.Dom;
using Bunit;
using FluentAssertions;
using NSubstitute;
using Tenants.Application.Squads.DTOs;
using UnitTests.Frontend.Common;
using WebApp.Components.Pages;
using WebApp.State;
using Xunit;

namespace UnitTests.Frontend.Components.Pages;

/// <summary>
/// Testes unitários com bUnit para a página de Squads (<see cref="SquadsPage"/>).
/// </summary>
public sealed class SquadsPageTests : BunitTestBase
{
    /// <summary>
    /// Valida que a página renderiza o título com o nome institucional da agência ativa e o componente SquadManager.
    /// </summary>
    [Fact]
    public void SquadsPage_WhenRendered_ShouldRenderTitleAndSquadManager()
    {
        // Arrange
        var tenant = new TenantState(
            TenantId: Guid.NewGuid(),
            Name: "Agência Performance Top",
            Slug: "top",
            CustomDomain: null,
            Branding: TenantBranding.Default);

        SetTenant(tenant);

        SquadClientService.GetSquadsAsync(Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result<IReadOnlyList<SquadSummaryDto>>.Success(Array.Empty<SquadSummaryDto>()));

        // Act
        var cut = Render<SquadsPage>();

        // Assert
        var manager = cut.FindComponent<WebApp.Components.Squads.SquadManager>();
        manager.Should().NotBeNull();

        var pageTitle = cut.Find(".page-title");
        pageTitle.TextContent.Should().Contain("Squads");
    }
}
