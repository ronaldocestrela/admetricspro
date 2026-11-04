using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SocialPublishing.Application.Traction.DTOs;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Traction;
using Bunit;
using UnitTests.Frontend.Common;
using WebApp.Components.Social;
using WebApp.Services.Social;
using Xunit;

namespace UnitTests.Frontend.Social;

/// <summary>
/// Testes bUnit do banner <see cref="BoostSuggestionBanner"/> (Subfase 6.4).
/// </summary>
public sealed class BoostSuggestionBannerTests : BunitTestBase
{
    private readonly ISocialPublishingClient _socialClient = Substitute.For<ISocialPublishingClient>();

    /// <summary>
    /// Registra mock do cliente social.
    /// </summary>
    public BoostSuggestionBannerTests()
    {
        Services.AddSingleton(_socialClient);
    }

    /// <summary>
    /// Sugestão Open exibe rede, múltiplo e ações Aceitar / Dispensar.
    /// </summary>
    [Fact]
    public async Task Banner_WhenOpenSuggestion_ShouldShowPlatformMultipleAndActions()
    {
        var workspaceId = Guid.NewGuid();
        var suggestionId = Guid.NewGuid();
        var suggestion = CreateSuggestion(suggestionId, PaidBoostSuggestionStatus.Open, 2.5m);

        _socialClient.ListBoostSuggestionsAsync(workspaceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<PaidBoostSuggestionDto>>.Success(new[] { suggestion })));

        var cut = RenderBanner(workspaceId);
        cut.WaitForState(() => cut.FindAll("[data-testid='boost-suggestion-banner']").Count > 0);

        cut.Markup.Should().Contain("Instagram");
        cut.Find("[data-testid='boost-suggestion-multiple']").TextContent.Should().Contain("2,5");
        cut.Find($"[data-testid='boost-suggestion-accept-{suggestionId}']").Should().NotBeNull();
        cut.Find($"[data-testid='boost-suggestion-dismiss-{suggestionId}']").Should().NotBeNull();
    }

    /// <summary>
    /// Aceitar com sucesso marca Accepted e exibe o id do rascunho; botões somem.
    /// </summary>
    [Fact]
    public async Task Banner_WhenAcceptSucceeds_ShouldShowAcceptedWithDraftId()
    {
        var workspaceId = Guid.NewGuid();
        var suggestionId = Guid.NewGuid();
        var draftId = Guid.NewGuid();
        var open = CreateSuggestion(suggestionId, PaidBoostSuggestionStatus.Open, 2.0m);
        var accepted = open with { Status = PaidBoostSuggestionStatus.Accepted, PaidDraftId = draftId };

        _socialClient.ListBoostSuggestionsAsync(workspaceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<PaidBoostSuggestionDto>>.Success(new[] { open })));
        _socialClient.AcceptBoostSuggestionAsync(workspaceId, suggestionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<PaidBoostSuggestionDto>.Success(accepted)));

        var cut = RenderBanner(workspaceId);
        cut.WaitForState(() => cut.FindAll($"[data-testid='boost-suggestion-accept-{suggestionId}']").Count > 0);

        await cut.Find($"[data-testid='boost-suggestion-accept-{suggestionId}']").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        await _socialClient.Received(1).AcceptBoostSuggestionAsync(workspaceId, suggestionId, Arg.Any<CancellationToken>());
        cut.WaitForState(() => cut.FindAll($"[data-testid='boost-suggestion-accepted-{suggestionId}']").Count > 0);
        cut.Markup.Should().Contain(draftId.ToString());
        cut.FindAll($"[data-testid='boost-suggestion-accept-{suggestionId}']").Count.Should().Be(0);
    }

    /// <summary>
    /// Falha no aceite mantém Open, botões e mensagem de erro.
    /// </summary>
    [Fact]
    public async Task Banner_WhenAcceptFails_ShouldStayOpenAndShowError()
    {
        var workspaceId = Guid.NewGuid();
        var suggestionId = Guid.NewGuid();
        var open = CreateSuggestion(suggestionId, PaidBoostSuggestionStatus.Open, 2.0m);

        _socialClient.ListBoostSuggestionsAsync(workspaceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<PaidBoostSuggestionDto>>.Success(new[] { open })));
        _socialClient.AcceptBoostSuggestionAsync(workspaceId, suggestionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<PaidBoostSuggestionDto>.Failure(
                Error.NotFound("Meta.AccountNotLinked", "Conta Meta não vinculada."))));

        var cut = RenderBanner(workspaceId);
        cut.WaitForState(() => cut.FindAll($"[data-testid='boost-suggestion-accept-{suggestionId}']").Count > 0);

        await cut.Find($"[data-testid='boost-suggestion-accept-{suggestionId}']").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        cut.Find("[data-testid='boost-suggestion-error']").TextContent.Should().Contain("Conta Meta não vinculada");
        cut.Find($"[data-testid='boost-suggestion-accept-{suggestionId}']").Should().NotBeNull();
    }

    /// <summary>
    /// Dispensar com sucesso marca Dismissed sem chamar aceite.
    /// </summary>
    [Fact]
    public async Task Banner_WhenDismissSucceeds_ShouldShowDismissedWithoutAccept()
    {
        var workspaceId = Guid.NewGuid();
        var suggestionId = Guid.NewGuid();
        var open = CreateSuggestion(suggestionId, PaidBoostSuggestionStatus.Open, 2.0m);
        var dismissed = open with { Status = PaidBoostSuggestionStatus.Dismissed };

        _socialClient.ListBoostSuggestionsAsync(workspaceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<PaidBoostSuggestionDto>>.Success(new[] { open })));
        _socialClient.DismissBoostSuggestionAsync(workspaceId, suggestionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<PaidBoostSuggestionDto>.Success(dismissed)));

        var cut = RenderBanner(workspaceId);
        cut.WaitForState(() => cut.FindAll($"[data-testid='boost-suggestion-dismiss-{suggestionId}']").Count > 0);

        await cut.Find($"[data-testid='boost-suggestion-dismiss-{suggestionId}']").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        await _socialClient.DidNotReceive().AcceptBoostSuggestionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        cut.WaitForState(() => cut.FindAll($"[data-testid='boost-suggestion-dismissed-{suggestionId}']").Count > 0);
    }

    /// <summary>
    /// Lista vazia não renderiza blocos de sugestão.
    /// </summary>
    [Fact]
    public async Task Banner_WhenNoSuggestions_ShouldNotRenderItems()
    {
        var workspaceId = Guid.NewGuid();
        _socialClient.ListBoostSuggestionsAsync(workspaceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<PaidBoostSuggestionDto>>.Success(Array.Empty<PaidBoostSuggestionDto>())));

        var cut = RenderBanner(workspaceId);
        await Task.Delay(50);
        cut.FindAll("[data-testid='boost-suggestion-item']").Count.Should().Be(0);
    }

    private IRenderedComponent<BoostSuggestionBanner> RenderBanner(Guid workspaceId) =>
        Render<BoostSuggestionBanner>(p => p.Add(x => x.WorkspaceId, workspaceId));

    private static PaidBoostSuggestionDto CreateSuggestion(
        Guid id,
        PaidBoostSuggestionStatus status,
        decimal multiple) =>
        new(
            id,
            Guid.NewGuid(),
            OrganicPlatform.Instagram,
            status,
            5000,
            0.05m,
            multiple,
            DateTime.UtcNow,
            status == PaidBoostSuggestionStatus.Accepted ? Guid.NewGuid() : null);
}
