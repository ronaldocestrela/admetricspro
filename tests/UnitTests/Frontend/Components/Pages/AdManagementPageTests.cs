using BuildingBlocks.Domain.Primitives;
using Bunit;
using FluentAssertions;
using NSubstitute;
using Tenants.Application.Workspaces.DTOs;
using UnitTests.Frontend.Common;
using WebApp.Components.Pages;

namespace UnitTests.Frontend.Components.Pages;

/// <summary>Verifica navegação dedicada de gerenciamento por workspace e plataforma.</summary>
public sealed class AdManagementPageTests : BunitTestBase
{
    /// <summary>Os quatro cards abrem a área da plataforma no workspace selecionado.</summary>
    [Fact]
    public void Page_WithWorkspace_RendersDedicatedPlatformLinks()
    {
        var workspace = Guid.NewGuid();
        WorkspaceClientService.GetWorkspacesAsync(Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<WorkspaceDto>>.Success([new(workspace, "Cliente", "", 100m, "", true, DateTime.UtcNow, null)]));
        var cut = Render<AdManagementPage>();
        foreach (var platform in new[] { "meta", "google", "tiktok", "bing" })
            cut.Find($"a[href='/ad-management/{workspace}/{platform}']").Should().NotBeNull();
    }

    /// <summary>Sem workspace, não cria links com identificador vazio.</summary>
    [Fact]
    public void Page_WithoutWorkspace_PromptsWorkspaceCreation()
    {
        var cut = Render<AdManagementPage>();
        cut.Markup.Should().Contain("Cadastre um workspace");
        cut.FindAll("a[href*='/ad-management/']").Should().BeEmpty();
    }
}
