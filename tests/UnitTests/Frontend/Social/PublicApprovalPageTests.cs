using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SocialPublishing.Application.Approvals;
using SocialPublishing.Application.Approvals.DTOs;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Brand;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Posts;
using UnitTests.Frontend.Common;
using WebApp.Components.Social;
using WebApp.Services.Social;
using Xunit;

namespace UnitTests.Frontend.Social;

/// <summary>
/// Testes bUnit de <see cref="PublicApprovalPage"/> (Subfase 5.3.4).
/// </summary>
public sealed class PublicApprovalPageTests : BunitTestBase
{
    private readonly IPublicSocialApprovalClient _client = Substitute.For<IPublicSocialApprovalClient>();

    /// <summary>
    /// Registra mock do cliente público.
    /// </summary>
    public PublicApprovalPageTests()
    {
        Services.AddSingleton(_client);
    }

    /// <summary>
    /// Aplica CSS variable de branding e não renderiza chrome de login.
    /// </summary>
    [Fact]
    public void Page_WithBoard_AppliesBrandingAndNoLoginChrome()
    {
        var postId = Guid.NewGuid();
        _client.GetBoardAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(
                Result<PublicApprovalBoardDto>.Success(new PublicApprovalBoardDto(
                new[]
                {
                    new PublicApprovalPostDto(
                        postId,
                        "Legenda teste",
                        OrganicPlatform.Instagram,
                        OrganicPostFormat.FeedImage,
                        ContentPillar.Authority,
                        DateTime.UtcNow),
                },
                "#ABCDEF",
                "https://logo.test/logo.png",
                "Agência QA")));

        var cut = Render<PublicApprovalPage>(p => p.Add(x => x.Token, "token"));

        cut.Find("[data-testid='public-approval-content']").GetAttribute("style")
            .Should().Contain("--brand-primary: #ABCDEF");
        cut.Markup.Should().NotContain("TenantLogin");
        cut.Markup.Should().NotContain("AdMetricsPro");
    }

    /// <summary>
    /// Botão aprovar envia ids selecionados.
    /// </summary>
    [Fact]
    public async Task Page_Approve_SendsSelectedPostIds()
    {
        var postId = Guid.NewGuid();
        _client.GetBoardAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(
                Result<PublicApprovalBoardDto>.Success(new PublicApprovalBoardDto(
                new[]
                {
                    new PublicApprovalPostDto(
                        postId,
                        "Legenda",
                        OrganicPlatform.Instagram,
                        OrganicPostFormat.FeedImage,
                        ContentPillar.Authority,
                        DateTime.UtcNow),
                },
                "#111111",
                null,
                "Agência")));

        _client.SubmitDecisionAsync(
                Arg.Any<string>(),
                Arg.Any<IReadOnlyList<Guid>>(),
                PublicApprovalDecision.Approve,
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var cut = Render<PublicApprovalPage>(p => p.Add(x => x.Token, "abc"));
        cut.WaitForAssertion(() => cut.Find("[data-testid='public-approval-approve']").Should().NotBeNull());

        await cut.Find("[data-testid='public-approval-approve']").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        await cut.InvokeAsync(() => Task.Delay(300));

        await _client.Received(1).SubmitDecisionAsync(
            Arg.Any<string>(),
            Arg.Is<IReadOnlyList<Guid>>(ids => ids.Contains(postId)),
            PublicApprovalDecision.Approve,
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Falha de carregamento mostra estado genérico indisponível.
    /// </summary>
    [Fact]
    public void Page_NotFound_ShowsGenericUnavailableState()
    {
        _client.GetBoardAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result<PublicApprovalBoardDto>.Failure(SocialErrors.ApprovalTokenInvalid));

        var cut = Render<PublicApprovalPage>(p => p.Add(x => x.Token, "missing"));

        cut.Find("[data-testid='public-approval-unavailable']").Should().NotBeNull();
        cut.Markup.Should().Contain("Link indisponível");
    }
}
