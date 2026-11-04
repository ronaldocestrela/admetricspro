using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SocialPublishing.Application.Posts.DTOs;
using SocialPublishing.Domain.Brand;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Posts;
using UnitTests.Frontend.Common;
using WebApp.Components.Social;
using WebApp.Services.Social;
using Xunit;

namespace UnitTests.Frontend.Social;

/// <summary>
/// Testes bUnit da grade <see cref="SocialCalendarGrid"/> e da página <see cref="SocialCalendarPage"/>.
/// </summary>
public sealed class SocialCalendarGridTests : BunitTestBase
{
    private const string TimeZoneId = "America/Sao_Paulo";
    private readonly ISocialPublishingClient _socialClient = Substitute.For<ISocialPublishingClient>();

    /// <summary>
    /// Registra mock do cliente social.
    /// </summary>
    public SocialCalendarGridTests()
    {
        Services.AddSingleton(_socialClient);
    }

    /// <summary>
    /// Modo Month renderiza células do mês civil com dias de preenchimento.
    /// </summary>
    [Fact]
    public void Grid_MonthView_ShouldRenderMonthCellsWithPaddingDays()
    {
        var workspaceId = Guid.NewGuid();
        var anchor = new DateOnly(2026, 11, 15);

        var cut = Render<SocialCalendarGrid>(p => p
            .Add(x => x.WorkspaceId, workspaceId)
            .Add(x => x.View, SocialCalendarView.Month)
            .Add(x => x.AnchorDate, anchor)
            .Add(x => x.TimeZone, TimeZoneId)
            .Add(x => x.Posts, Array.Empty<OrganicPostDto>()));

        cut.Find("[data-testid='calendar-day-2026-10-26']").Should().NotBeNull();
        cut.Find("[data-testid='calendar-day-2026-11-01']").Should().NotBeNull();
        cut.Find("[data-testid='calendar-day-2026-11-30']").Should().NotBeNull();
        cut.Find("[data-testid='calendar-day-2026-12-06']").Should().NotBeNull();
        cut.FindAll(".social-calendar-weekday").Count.Should().Be(7);
    }

    /// <summary>
    /// Modo Week renderiza exatamente sete colunas da semana da âncora.
    /// </summary>
    [Fact]
    public void Grid_WeekView_ShouldRenderSevenWeekColumns()
    {
        var workspaceId = Guid.NewGuid();
        var anchor = new DateOnly(2026, 11, 3);

        var cut = Render<SocialCalendarGrid>(p => p
            .Add(x => x.WorkspaceId, workspaceId)
            .Add(x => x.View, SocialCalendarView.Week)
            .Add(x => x.AnchorDate, anchor)
            .Add(x => x.TimeZone, TimeZoneId)
            .Add(x => x.Posts, Array.Empty<OrganicPostDto>()));

        cut.FindAll("[data-testid^='calendar-day-']").Count.Should().Be(7);
        cut.Find("[data-testid='calendar-day-2026-11-02']").Should().NotBeNull();
        cut.Find("[data-testid='calendar-day-2026-11-08']").Should().NotBeNull();
    }

    /// <summary>
    /// Post aparece no dia civil convertido a partir do UTC.
    /// </summary>
    [Fact]
    public void Grid_ShouldPlacePostOnLocalDayFromUtc()
    {
        var workspaceId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var post = CreatePost(postId, workspaceId, new DateTime(2026, 11, 3, 13, 0, 0, DateTimeKind.Utc), OrganicPostStatus.Scheduled);

        var cut = Render<SocialCalendarGrid>(p => p
            .Add(x => x.WorkspaceId, workspaceId)
            .Add(x => x.View, SocialCalendarView.Month)
            .Add(x => x.AnchorDate, new DateOnly(2026, 11, 1))
            .Add(x => x.TimeZone, TimeZoneId)
            .Add(x => x.Posts, new[] { post }));

        var dayCell = cut.Find("[data-testid='calendar-day-2026-11-03']");
        dayCell.QuerySelector($"[data-testid='post-card-{postId}']").Should().NotBeNull();
    }

    /// <summary>
    /// Post publicado não expõe alça de arraste.
    /// </summary>
    [Fact]
    public void Grid_PublishedPost_ShouldNotExposeDragHandle()
    {
        var workspaceId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var post = CreatePost(postId, workspaceId, new DateTime(2026, 11, 3, 13, 0, 0, DateTimeKind.Utc), OrganicPostStatus.Published);

        var cut = Render<SocialCalendarGrid>(p => p
            .Add(x => x.WorkspaceId, workspaceId)
            .Add(x => x.View, SocialCalendarView.Month)
            .Add(x => x.AnchorDate, new DateOnly(2026, 11, 1))
            .Add(x => x.TimeZone, TimeZoneId)
            .Add(x => x.Posts, new[] { post }));

        cut.FindAll($"[data-testid='drag-handle-{postId}']").Count.Should().Be(0);
    }

    /// <summary>
    /// Soltar post agendado em outro dia chama RescheduleAsync com UTC preservando hora local.
    /// </summary>
    [Fact]
    public async Task Grid_WhenDropScheduledPost_ShouldCallRescheduleWithPreservedLocalTime()
    {
        var workspaceId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var scheduledUtc = new DateTime(2026, 11, 3, 13, 0, 0, DateTimeKind.Utc);
        var post = CreatePost(postId, workspaceId, scheduledUtc, OrganicPostStatus.Scheduled);
        var expectedUtc = new DateTime(2026, 11, 10, 13, 0, 0, DateTimeKind.Utc);

        DateTime capturedUtc = default;
        _socialClient.RescheduleAsync(
                workspaceId,
                postId,
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                capturedUtc = call.ArgAt<DateTime>(2);
                return Task.FromResult(Result<OrganicPostDto>.Success(post with { ScheduledAtUtc = capturedUtc }));
            });

        var cut = Render<SocialCalendarGrid>(p => p
            .Add(x => x.WorkspaceId, workspaceId)
            .Add(x => x.View, SocialCalendarView.Month)
            .Add(x => x.AnchorDate, new DateOnly(2026, 11, 1))
            .Add(x => x.TimeZone, TimeZoneId)
            .Add(x => x.Posts, new[] { post }));

        await cut.Find($"[data-testid='post-card-{postId}']").TriggerEventAsync("ondragstart", new Microsoft.AspNetCore.Components.Web.DragEventArgs());
        await cut.Find("[data-testid='calendar-day-2026-11-10']").TriggerEventAsync("ondrop", new Microsoft.AspNetCore.Components.Web.DragEventArgs());

        await _socialClient.Received(1).RescheduleAsync(
            workspaceId,
            postId,
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());

        capturedUtc.Should().Be(expectedUtc);
    }

    /// <summary>
    /// Falha ao reagendar reverte o card e exibe a mensagem de erro.
    /// </summary>
    [Fact]
    public async Task Grid_WhenRescheduleFails_ShouldRevertPostAndShowError()
    {
        var workspaceId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var post = CreatePost(postId, workspaceId, new DateTime(2026, 11, 3, 13, 0, 0, DateTimeKind.Utc), OrganicPostStatus.Scheduled);

        _socialClient.RescheduleAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<OrganicPostDto>.Failure(SocialPublishing.Domain.SocialErrors.SlotNotReschedulable)));

        var cut = Render<SocialCalendarGrid>(p => p
            .Add(x => x.WorkspaceId, workspaceId)
            .Add(x => x.View, SocialCalendarView.Month)
            .Add(x => x.AnchorDate, new DateOnly(2026, 11, 1))
            .Add(x => x.TimeZone, TimeZoneId)
            .Add(x => x.Posts, new[] { post }));

        await cut.Find($"[data-testid='post-card-{postId}']").TriggerEventAsync("ondragstart", new Microsoft.AspNetCore.Components.Web.DragEventArgs());
        await cut.Find("[data-testid='calendar-day-2026-11-10']").TriggerEventAsync("ondrop", new Microsoft.AspNetCore.Components.Web.DragEventArgs());

        cut.Find("[data-testid='calendar-day-2026-11-03']").QuerySelector($"[data-testid='post-card-{postId}']").Should().NotBeNull();
        cut.Find("[data-testid='social-calendar-error']").TextContent.Should().Contain("não pode ser reagendado");
    }

    /// <summary>
    /// Troca de workspace recarrega a grade sem vazar posts do cliente anterior.
    /// </summary>
    [Fact]
    public async Task Page_WhenWorkspaceChanges_ShouldReloadPostsWithoutLeak()
    {
        var workspaceA = Guid.NewGuid();
        var workspaceB = Guid.NewGuid();
        var postA = CreatePost(Guid.NewGuid(), workspaceA, new DateTime(2026, 11, 3, 13, 0, 0, DateTimeKind.Utc), OrganicPostStatus.Scheduled);

        _socialClient.ListPostsAsync(workspaceA, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<OrganicPostDto>>.Success(new[] { postA })));
        _socialClient.ListPostsAsync(workspaceB, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<OrganicPostDto>>.Success(Array.Empty<OrganicPostDto>())));

        await WorkspaceContextStateProvider.SetActiveWorkspaceAsync(workspaceA, "Cliente A");
        var cut = Render<SocialCalendarPage>();
        cut.WaitForAssertion(() => cut.FindAll($"[data-testid='post-card-{postA.Id}']").Count.Should().Be(1));

        await WorkspaceContextStateProvider.SetActiveWorkspaceAsync(workspaceB, "Cliente B");
        cut.WaitForAssertion(() => cut.FindAll($"[data-testid='post-card-{postA.Id}']").Count.Should().Be(0));
    }

    /// <summary>
    /// Sem workspace selecionado exibe alerta orientando seleção na barra lateral.
    /// </summary>
    [Fact]
    public void Page_WhenNoWorkspace_ShouldShowSelectWorkspaceAlert()
    {
        WorkspaceContextStateProvider.Clear();
        var cut = Render<SocialCalendarPage>();
        cut.Find("[data-testid='social-calendar-select-workspace']").Should().NotBeNull();
    }

    private static OrganicPostDto CreatePost(
        Guid id,
        Guid workspaceId,
        DateTime scheduledAtUtc,
        OrganicPostStatus status) =>
        new(
            id,
            workspaceId,
            Guid.NewGuid(),
            OrganicPlatform.Instagram,
            OrganicPostFormat.FeedImage,
            ContentPillar.Authority,
            "Legenda de teste",
            scheduledAtUtc,
            status);
}
