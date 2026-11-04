using Analytics.Application.Creatives.DTOs;
using Bunit;
using FluentAssertions;
using UnitTests.Frontend.Common;
using WebApp.Components.Creatives;
using Xunit;

namespace UnitTests.Frontend.Components.Creatives;

/// <summary>
/// Testes bUnit do card de ativo orgânico no Creative Hub (Subfase 6.4).
/// </summary>
public sealed class CreativeHubOrganicAssetCardTests : BunitTestBase
{
    /// <summary>
    /// Ativo com permalink renderiza link público.
    /// </summary>
    [Fact]
    public void OrganicAssetCard_ShouldRenderPermalinkLink_WhenPermalinkPresent()
    {
        var assetId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var asset = new CreativeHubOrganicAssetDto(
            assetId,
            postId,
            "Instagram",
            "https://instagram.com/p/abc123",
            2.5m);

        var cut = Render<CreativeHubOrganicAssetCard>(p => p.Add(x => x.Asset, asset));

        var link = cut.Find("[data-testid='organic-asset-permalink']");
        link.GetAttribute("href").Should().Be("https://instagram.com/p/abc123");
    }

    /// <summary>
    /// Permalink ausente não renderiza link quebrado.
    /// </summary>
    [Fact]
    public void OrganicAssetCard_WhenPermalinkMissing_ShouldNotRenderBrokenLink()
    {
        var asset = new CreativeHubOrganicAssetDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Instagram",
            null,
            2.0m);

        var cut = Render<CreativeHubOrganicAssetCard>(p => p.Add(x => x.Asset, asset));

        cut.FindAll("[data-testid='organic-asset-permalink']").Count.Should().Be(0);
    }
}
