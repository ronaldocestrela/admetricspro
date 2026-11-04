using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SocialPublishing.Application.Planning.DTOs;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Brand;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Posts;
using Bunit;
using UnitTests.Frontend.Common;
using WebApp.Components.Social;
using WebApp.Services.Social;
using Xunit;

namespace UnitTests.Frontend.Social;

/// <summary>
/// Testes bUnit do diálogo <see cref="GeneratePlanDialog"/>.
/// </summary>
public sealed class GeneratePlanDialogTests : BunitTestBase
{
    private const string TimeZoneId = "America/Sao_Paulo";
    private readonly ISocialPublishingClient _socialClient = Substitute.For<ISocialPublishingClient>();

    /// <summary>
    /// Registra mock do cliente social.
    /// </summary>
    public GeneratePlanDialogTests()
    {
        Services.AddSingleton(_socialClient);
    }

    /// <summary>
    /// Confirmar chama GeneratePlanAsync com intervalo, semente e posts por semana.
    /// </summary>
    [Fact]
    public async Task Dialog_WhenConfirm_ShouldCallGeneratePlanWithParameters()
    {
        var workspaceId = Guid.NewGuid();
        var rangeStart = new DateOnly(2026, 11, 2);
        var rangeEnd = new DateOnly(2026, 11, 6);

        _socialClient.GeneratePlanAsync(
                workspaceId,
                rangeStart,
                rangeEnd,
                TimeZoneId,
                5,
                42,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<GeneratedPlanDto>.Success(
                new GeneratedPlanDto(Array.Empty<Guid>(), Array.Empty<FailedEditorialSlotDto>()))));

        var cut = RenderDialog(workspaceId);
        await SetDateAsync(cut, "generate-plan-range-start", "2026-11-02");
        await SetDateAsync(cut, "generate-plan-range-end", "2026-11-06");
        await cut.Find("[data-testid='generate-plan-posts-per-week']").ChangeAsync("5");
        await cut.Find("[data-testid='generate-plan-seed']").ChangeAsync("42");

        await cut.Find("[data-testid='generate-plan-confirm']").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        await _socialClient.Received(1).GeneratePlanAsync(
            workspaceId,
            rangeStart,
            rangeEnd,
            TimeZoneId,
            5,
            42,
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Sucesso parcial lista slots falhos e mantém link para o calendário.
    /// </summary>
    [Fact]
    public async Task Dialog_WhenPartialSuccess_ShouldListFailedSlotsAndCalendarLink()
    {
        var workspaceId = Guid.NewGuid();
        var createdId = Guid.NewGuid();
        var failed = new FailedEditorialSlotDto(
            new DateOnly(2026, 11, 5),
            new DateTime(2026, 11, 5, 13, 0, 0, DateTimeKind.Utc),
            ContentPillar.SocialProof,
            OrganicPlatform.TikTok,
            OrganicPostFormat.TikTokVideo,
            "Social.CopyGenerationFailed",
            "Falha do provedor de IA.");

        _socialClient.GeneratePlanAsync(
                Arg.Any<Guid>(),
                Arg.Any<DateOnly>(),
                Arg.Any<DateOnly>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<GeneratedPlanDto>.Success(
                new GeneratedPlanDto(new[] { createdId }, new[] { failed }))));

        var cut = RenderDialog(workspaceId);
        await cut.Find("[data-testid='generate-plan-confirm']").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='generate-plan-created-summary']").TextContent.Should().Contain("1 post");
            cut.Find("[data-testid='generate-plan-created-link']").GetAttribute("href").Should().Be("social/calendar");
            cut.Find("[data-testid='generate-plan-failed-slots']").Should().NotBeNull();
            cut.Find("[data-testid='generate-plan-failed-slot-2026-11-05']").TextContent.Should().Contain("Falha do provedor");
        });
    }

    /// <summary>
    /// Falha total exibe erro e não mostra slots nem link de posts criados.
    /// </summary>
    [Fact]
    public async Task Dialog_WhenGenerationFails_ShouldShowErrorWithoutSlotsOrLink()
    {
        var workspaceId = Guid.NewGuid();
        _socialClient.GeneratePlanAsync(
                Arg.Any<Guid>(),
                Arg.Any<DateOnly>(),
                Arg.Any<DateOnly>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<GeneratedPlanDto>.Failure(SocialErrors.BrandVoiceNotFound)));

        var cut = RenderDialog(workspaceId);
        await cut.Find("[data-testid='generate-plan-confirm']").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='generate-plan-error']").Should().NotBeNull();
            cut.FindAll("[data-testid='generate-plan-failed-slots']").Count.Should().Be(0);
            cut.FindAll("[data-testid='generate-plan-created-link']").Count.Should().Be(0);
        });
    }

    private IRenderedComponent<GeneratePlanDialog> RenderDialog(Guid workspaceId) =>
        Render<GeneratePlanDialog>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.WorkspaceId, workspaceId)
            .Add(x => x.TimeZoneId, TimeZoneId));

    private static async Task SetDateAsync(IRenderedComponent<GeneratePlanDialog> cut, string testId, string isoDate)
    {
        var host = cut.Find($"[data-testid='{testId}']");
        var input = host.QuerySelector("input") ?? host;
        await input.ChangeAsync(isoDate);
    }
}
