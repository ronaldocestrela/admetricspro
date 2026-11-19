using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SocialPublishing.Application.Approvals.DTOs;
using SocialPublishing.Application.Posts.DTOs;
using SocialPublishing.Application.Traction.DTOs;
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
        _socialClient.ListBoostSuggestionsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<PaidBoostSuggestionDto>>.Success(Array.Empty<PaidBoostSuggestionDto>())));
    }

    /// <summary>
    /// A grade inclui o banner de sugestão de impulsionamento como filho.
    /// </summary>
    [Fact]
    public void Grid_ShouldRenderBoostSuggestionBannerChild()
    {
        var workspaceId = Guid.NewGuid();
        _socialClient.ListBoostSuggestionsAsync(workspaceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<PaidBoostSuggestionDto>>.Success(Array.Empty<PaidBoostSuggestionDto>())));

        var cut = Render<SocialCalendarGrid>(p => p
            .Add(x => x.WorkspaceId, workspaceId)
            .Add(x => x.View, SocialCalendarView.Month)
            .Add(x => x.AnchorDate, new DateOnly(2026, 11, 1))
            .Add(x => x.TimeZone, TimeZoneId)
            .Add(x => x.Posts, Array.Empty<OrganicPostDto>()));

        cut.Find("[data-testid='boost-suggestion-banner-host']").Should().NotBeNull();
        _socialClient.Received(1).ListBoostSuggestionsAsync(workspaceId, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Modo Month renderiza células do mês civil com dias de preenchimento.
    /// </summary>
    [Fact]
    public void Grid_MonthView_ShouldRenderMonthCellsWithPaddingDays()
    {
        var workspaceId = Guid.NewGuid();
        _socialClient.ListBoostSuggestionsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<PaidBoostSuggestionDto>>.Success(Array.Empty<PaidBoostSuggestionDto>())));
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
        _socialClient.ListBoostSuggestionsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<PaidBoostSuggestionDto>>.Success(Array.Empty<PaidBoostSuggestionDto>())));
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
    /// Post reagendável expõe atributo draggable em string literal "true".
    /// </summary>
    [Fact]
    public void Grid_ScheduledPost_ShouldRenderDraggableTrue()
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

        cut.Find($"[data-testid='post-card-{postId}']").GetAttribute("draggable").Should().Be("true");
    }

    /// <summary>
    /// Post publicado renderiza draggable="false" e não expõe alça de arraste.
    /// </summary>
    [Fact]
    public void Grid_PublishedPost_ShouldRenderDraggableFalse()
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

        cut.Find($"[data-testid='post-card-{postId}']").GetAttribute("draggable").Should().Be("false");
        cut.FindAll($"[data-testid='drag-handle-{postId}']").Count.Should().Be(0);
    }

    /// <summary>
    /// Clique no card abre o diálogo de detalhe com legenda completa e metadados.
    /// </summary>
    [Fact]
    public void Grid_WhenCardClicked_ShouldOpenDetailWithFullCaptionAndMetadata()
    {
        var workspaceId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var longCaption = new string('A', 100);
        var scheduledUtc = new DateTime(2026, 11, 3, 16, 0, 0, DateTimeKind.Utc);
        var post = CreatePost(postId, workspaceId, scheduledUtc, OrganicPostStatus.Scheduled, longCaption);

        var cut = Render<SocialCalendarGrid>(p => p
            .Add(x => x.WorkspaceId, workspaceId)
            .Add(x => x.View, SocialCalendarView.Month)
            .Add(x => x.AnchorDate, new DateOnly(2026, 11, 1))
            .Add(x => x.TimeZone, TimeZoneId)
            .Add(x => x.Posts, new[] { post }));

        cut.Find($"[data-testid='post-card-{postId}']").Click();

        cut.Find("[data-testid='organic-post-detail-dialog']").Should().NotBeNull();
        cut.Find("[data-testid='organic-post-detail-caption-body']").TextContent.Should().Be(longCaption);
        cut.Find("[data-testid='organic-post-detail-meta-platform']").TextContent.Should().Contain("Instagram");
        cut.Find("[data-testid='organic-post-detail-meta-format']").TextContent.Should().Contain("Imagem");
        cut.Find("[data-testid='organic-post-detail-meta-scheduled']").TextContent.Should().Contain("03/11/2026");
        cut.Find("[data-testid='organic-post-status-badge']").TextContent.Should().Contain("Agendado");
    }

    /// <summary>
    /// Legenda com <c>\n</c> literal não exibe o escape e separa hashtags em chips.
    /// </summary>
    [Fact]
    public void Grid_WhenDetailOpened_WithLiteralNewlines_ShouldFormatCaptionAndMetadata()
    {
        var workspaceId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var caption = "Linha um\\n\\nLinha dois\\n\\n#Marca #Nicho";
        var scheduledUtc = new DateTime(2026, 11, 19, 13, 0, 0, DateTimeKind.Utc);
        var post = CreatePost(postId, workspaceId, scheduledUtc, OrganicPostStatus.Draft, caption);

        var cut = Render<SocialCalendarGrid>(p => p
            .Add(x => x.WorkspaceId, workspaceId)
            .Add(x => x.View, SocialCalendarView.Month)
            .Add(x => x.AnchorDate, new DateOnly(2026, 11, 1))
            .Add(x => x.TimeZone, TimeZoneId)
            .Add(x => x.Posts, new[] { post }));

        cut.Find($"[data-testid='post-card-{postId}']").Click();

        cut.Find("[data-testid='organic-post-detail-caption-body']").TextContent.Should().NotContain("\\n");
        cut.Find("[data-testid='organic-post-detail-caption-body']").TextContent.Should().Contain("Linha um");
        cut.Find("[data-testid='organic-post-detail-caption-body']").TextContent.Should().Contain("Linha dois");
        cut.FindAll("[data-testid='organic-post-detail-hashtag']").Count.Should().Be(2);

        cut.Find("[data-testid='organic-post-detail-status-row']").Should().NotBeNull();
        cut.Find("[data-testid='organic-post-detail-pillar-row']").Should().NotBeNull();
        cut.Find("[data-testid='organic-post-detail-meta-platform']").Should().NotBeNull();
        cut.Find("[data-testid='organic-post-detail-meta-format']").Should().NotBeNull();
        cut.Find("[data-testid='organic-post-detail-meta-scheduled']").Should().NotBeNull();
    }

    /// <summary>
    /// Prévia do card não exibe sequência literal <c>\n</c>.
    /// </summary>
    [Fact]
    public void Grid_CardPreview_ShouldCollapseLiteralNewlines()
    {
        var workspaceId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var caption = "Abertura\\n\\nContinuação";
        var post = CreatePost(postId, workspaceId, new DateTime(2026, 11, 19, 13, 0, 0, DateTimeKind.Utc), OrganicPostStatus.Draft, caption);

        var cut = Render<SocialCalendarGrid>(p => p
            .Add(x => x.WorkspaceId, workspaceId)
            .Add(x => x.View, SocialCalendarView.Month)
            .Add(x => x.AnchorDate, new DateOnly(2026, 11, 1))
            .Add(x => x.TimeZone, TimeZoneId)
            .Add(x => x.Posts, new[] { post }));

        cut.Find($"[data-testid='post-card-{postId}'] .social-calendar-post-caption").TextContent
            .Should().NotContain("\\n")
            .And.Contain("Abertura")
            .And.Contain("Continuação");
    }

    /// <summary>
    /// Fechar o diálogo de detalhe remove o painel.
    /// </summary>
    [Fact]
    public void Grid_WhenDetailClosed_ShouldRemoveDialog()
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

        cut.Find($"[data-testid='post-card-{postId}']").Click();
        cut.Find("[data-testid='organic-post-detail-close']").Click();
        cut.FindAll("[data-testid='organic-post-detail-dialog']").Count.Should().Be(0);
    }

    /// <summary>
    /// Clique imediatamente após dragstart não abre o detalhe (supressão pós-arraste).
    /// </summary>
    [Fact]
    public async Task Grid_WhenClickAfterDragStart_ShouldNotOpenDetail()
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

        await cut.Find($"[data-testid='post-card-{postId}']").TriggerEventAsync("ondragstart", new Microsoft.AspNetCore.Components.Web.DragEventArgs());
        await cut.Find($"[data-testid='post-card-{postId}']").TriggerEventAsync("ondragend", new Microsoft.AspNetCore.Components.Web.DragEventArgs());
        cut.Find($"[data-testid='post-card-{postId}']").Click();

        cut.FindAll("[data-testid='organic-post-detail-dialog']").Count.Should().Be(0);
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
    /// Botão de link de aprovação chama o cliente autenticado.
    /// </summary>
    [Fact]
    public async Task Page_CreateApprovalLink_CallsClient()
    {
        var workspaceId = Guid.NewGuid();
        _socialClient.ListPostsAsync(workspaceId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<OrganicPostDto>>.Success(Array.Empty<OrganicPostDto>())));
        _socialClient.CreateApprovalLinkAsync(workspaceId, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Result<ApprovalLinkCreatedDto>.Success(
                new ApprovalLinkCreatedDto("plain-token", DateTime.UtcNow.AddDays(7), "/approve/plain-token")));

        await WorkspaceContextStateProvider.SetActiveWorkspaceAsync(workspaceId, "Cliente");
        var cut = Render<SocialCalendarPage>();
        cut.Find("[data-testid='calendar-create-approval-link']").Click();

        await _socialClient.Received().CreateApprovalLinkAsync(
            workspaceId,
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
        cut.WaitForAssertion(() => cut.Find("[data-testid='calendar-approval-link-result']").Should().NotBeNull());
    }

    /// <summary>
    /// Aprovar rascunho chama submit e approve; o selo do card passa a Aprovado.
    /// </summary>
    [Fact]
    public async Task Grid_WhenApproveDraft_ShouldSubmitThenApproveAndUpdateCardBadge()
    {
        var workspaceId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var scheduledUtc = new DateTime(2026, 11, 19, 13, 0, 0, DateTimeKind.Utc);
        var post = CreatePost(postId, workspaceId, scheduledUtc, OrganicPostStatus.Draft);
        var pending = post with { Status = OrganicPostStatus.PendingApproval };
        var approved = post with { Status = OrganicPostStatus.Approved };

        _socialClient.SubmitForApprovalAsync(workspaceId, postId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<OrganicPostDto>.Success(pending)));
        _socialClient.ApprovePostAsync(workspaceId, postId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<OrganicPostDto>.Success(approved)));

        var cut = Render<SocialCalendarGrid>(p => p
            .Add(x => x.WorkspaceId, workspaceId)
            .Add(x => x.View, SocialCalendarView.Month)
            .Add(x => x.AnchorDate, new DateOnly(2026, 11, 1))
            .Add(x => x.TimeZone, TimeZoneId)
            .Add(x => x.Posts, new[] { post }));

        cut.Find($"[data-testid='post-card-{postId}']").Click();
        await cut.Find("[data-testid='organic-post-detail-approve']").ClickAsync();

        await _socialClient.Received(1).SubmitForApprovalAsync(workspaceId, postId, Arg.Any<CancellationToken>());
        await _socialClient.Received(1).ApprovePostAsync(workspaceId, postId, Arg.Any<CancellationToken>());

        cut.WaitForAssertion(() =>
            cut.Find($"[data-testid='post-card-{postId}'] [data-testid='organic-post-status-badge']").TextContent
                .Should().Contain("Aprovado"));
    }

    /// <summary>
    /// Falha no submit não chama approve e mantém status Rascunho.
    /// </summary>
    [Fact]
    public async Task Grid_WhenApproveDraftSubmitFails_ShouldNotCallApproveAndShowError()
    {
        var workspaceId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var post = CreatePost(postId, workspaceId, new DateTime(2026, 11, 19, 13, 0, 0, DateTimeKind.Utc), OrganicPostStatus.Draft);

        _socialClient.SubmitForApprovalAsync(workspaceId, postId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<OrganicPostDto>.Failure(SocialPublishing.Domain.SocialErrors.InvalidTransition)));

        var cut = Render<SocialCalendarGrid>(p => p
            .Add(x => x.WorkspaceId, workspaceId)
            .Add(x => x.View, SocialCalendarView.Month)
            .Add(x => x.AnchorDate, new DateOnly(2026, 11, 1))
            .Add(x => x.TimeZone, TimeZoneId)
            .Add(x => x.Posts, new[] { post }));

        cut.Find($"[data-testid='post-card-{postId}']").Click();
        await cut.Find("[data-testid='organic-post-detail-approve']").ClickAsync();

        await _socialClient.DidNotReceive().ApprovePostAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        cut.Find("[data-testid='organic-post-detail-error']").Should().NotBeNull();
        cut.Find($"[data-testid='post-card-{postId}'] [data-testid='organic-post-status-badge']").TextContent.Should().Contain("Rascunho");
    }

    /// <summary>
    /// Submit ok e approve falha: status Aguardando aprovação; segundo clique chama só approve.
    /// </summary>
    [Fact]
    public async Task Grid_WhenApproveDraftApproveFails_SecondClickApprovesOnly()
    {
        var workspaceId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var post = CreatePost(postId, workspaceId, new DateTime(2026, 11, 19, 13, 0, 0, DateTimeKind.Utc), OrganicPostStatus.Draft);
        var pending = post with { Status = OrganicPostStatus.PendingApproval };
        var approved = post with { Status = OrganicPostStatus.Approved };

        _socialClient.SubmitForApprovalAsync(workspaceId, postId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<OrganicPostDto>.Success(pending)));
        _socialClient.ApprovePostAsync(workspaceId, postId, Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(Result<OrganicPostDto>.Failure(SocialPublishing.Domain.SocialErrors.InvalidTransition)),
                Task.FromResult(Result<OrganicPostDto>.Success(approved)));

        var cut = Render<SocialCalendarGrid>(p => p
            .Add(x => x.WorkspaceId, workspaceId)
            .Add(x => x.View, SocialCalendarView.Month)
            .Add(x => x.AnchorDate, new DateOnly(2026, 11, 1))
            .Add(x => x.TimeZone, TimeZoneId)
            .Add(x => x.Posts, new[] { post }));

        cut.Find($"[data-testid='post-card-{postId}']").Click();
        await cut.Find("[data-testid='organic-post-detail-approve']").ClickAsync();

        await _socialClient.Received(1).SubmitForApprovalAsync(workspaceId, postId, Arg.Any<CancellationToken>());
        await _socialClient.Received(1).ApprovePostAsync(workspaceId, postId, Arg.Any<CancellationToken>());
        cut.Find("[data-testid='organic-post-detail-error']").Should().NotBeNull();
        cut.WaitForAssertion(() =>
            cut.Find($"[data-testid='post-card-{postId}'] [data-testid='organic-post-status-badge']").TextContent
                .Should().Contain("Aguardando aprovação"));

        await cut.Find("[data-testid='organic-post-detail-approve']").ClickAsync();

        await _socialClient.Received(1).SubmitForApprovalAsync(workspaceId, postId, Arg.Any<CancellationToken>());
        await _socialClient.Received(2).ApprovePostAsync(workspaceId, postId, Arg.Any<CancellationToken>());
        cut.WaitForAssertion(() =>
            cut.Find($"[data-testid='post-card-{postId}'] [data-testid='organic-post-status-badge']").TextContent
                .Should().Contain("Aprovado"));
    }

    /// <summary>
    /// Post já em PendingApproval: Aprovar chama só approve.
    /// </summary>
    [Fact]
    public async Task Grid_WhenApprovePendingApproval_ShouldCallApproveOnly()
    {
        var workspaceId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var post = CreatePost(postId, workspaceId, new DateTime(2026, 11, 19, 13, 0, 0, DateTimeKind.Utc), OrganicPostStatus.PendingApproval);
        var approved = post with { Status = OrganicPostStatus.Approved };

        _socialClient.ApprovePostAsync(workspaceId, postId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<OrganicPostDto>.Success(approved)));

        var cut = Render<SocialCalendarGrid>(p => p
            .Add(x => x.WorkspaceId, workspaceId)
            .Add(x => x.View, SocialCalendarView.Month)
            .Add(x => x.AnchorDate, new DateOnly(2026, 11, 1))
            .Add(x => x.TimeZone, TimeZoneId)
            .Add(x => x.Posts, new[] { post }));

        cut.Find($"[data-testid='post-card-{postId}']").Click();
        await cut.Find("[data-testid='organic-post-detail-approve']").ClickAsync();

        await _socialClient.DidNotReceive().SubmitForApprovalAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _socialClient.Received(1).ApprovePostAsync(workspaceId, postId, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Post agendado, aprovado ou publicado não exibe botão Aprovar no detalhe.
    /// </summary>
    [Theory]
    [InlineData(OrganicPostStatus.Approved)]
    [InlineData(OrganicPostStatus.Scheduled)]
    [InlineData(OrganicPostStatus.Published)]
    public void Grid_WhenDetailOpened_ForNonApprovableStatus_ShouldNotShowApproveButton(OrganicPostStatus status)
    {
        var workspaceId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var post = CreatePost(postId, workspaceId, new DateTime(2026, 11, 19, 13, 0, 0, DateTimeKind.Utc), status);

        var cut = Render<SocialCalendarGrid>(p => p
            .Add(x => x.WorkspaceId, workspaceId)
            .Add(x => x.View, SocialCalendarView.Month)
            .Add(x => x.AnchorDate, new DateOnly(2026, 11, 1))
            .Add(x => x.TimeZone, TimeZoneId)
            .Add(x => x.Posts, new[] { post }));

        cut.Find($"[data-testid='post-card-{postId}']").Click();
        cut.FindAll("[data-testid='organic-post-detail-approve']").Count.Should().Be(0);
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
        OrganicPostStatus status,
        string? caption = null) =>
        new(
            id,
            workspaceId,
            Guid.NewGuid(),
            OrganicPlatform.Instagram,
            OrganicPostFormat.FeedImage,
            ContentPillar.Authority,
            caption ?? "Legenda de teste",
            scheduledAtUtc,
            status);
}
