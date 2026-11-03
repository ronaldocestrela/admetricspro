using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Brand;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Posts;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Domain;

/// <summary>
/// Testes unitários do agregado <see cref="OrganicPost"/>.
/// </summary>
public sealed class OrganicPostTests
{
    private const string SaoPauloTimeZone = "America/Sao_Paulo";

    /// <summary>
    /// Valida que a criação inicializa em Draft.
    /// </summary>
    [Fact]
    public void Create_ComDadosValidos_DeveIniciarEmDraft()
    {
        var result = CreateDraftPost();

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(OrganicPostStatus.Draft);
        result.Value.ScheduledAtUtc.Kind.Should().Be(DateTimeKind.Utc);
    }

    /// <summary>
    /// Valida teto de legenda por rede.
    /// </summary>
    [Theory]
    [InlineData(OrganicPlatform.Instagram)]
    [InlineData(OrganicPlatform.FacebookPage)]
    [InlineData(OrganicPlatform.TikTok)]
    public void Create_ComLegendaAcimaDoTeto_DeveRetornarCaptionTooLong(OrganicPlatform platform)
    {
        var limit = CaptionLimits.GetMaxLength(platform);
        var caption = new string('x', limit + 1);

        var result = OrganicPost.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            platform,
            OrganicPostFormat.FeedImage,
            ContentPillar.Authority,
            caption,
            new DateTime(2026, 6, 2, 10, 0, 0),
            SaoPauloTimeZone);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.CaptionTooLong.Code);
    }

    /// <summary>
    /// Valida quantidade de itens do carrossel.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(11)]
    public void Create_CarrosselComContagemInvalida_DeveRetornarCarouselItemCountInvalid(int itemCount)
    {
        var media = Enumerable.Range(0, itemCount)
            .Select(i => (1080, 1080, i))
            .ToList();

        var result = OrganicPost.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            OrganicPlatform.Instagram,
            OrganicPostFormat.FeedCarousel,
            ContentPillar.Connection,
            "Legenda",
            new DateTime(2026, 6, 2, 10, 0, 0),
            SaoPauloTimeZone,
            media);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.CarouselItemCountInvalid.Code);
    }

    /// <summary>
    /// Valida limites válidos do carrossel.
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(10)]
    public void Create_CarrosselComContagemValida_DeveTerSucesso(int itemCount)
    {
        var media = Enumerable.Range(0, itemCount)
            .Select(i => (1080, 1080, i))
            .ToList();

        var result = OrganicPost.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            OrganicPlatform.Instagram,
            OrganicPostFormat.FeedCarousel,
            ContentPillar.Connection,
            "Legenda",
            new DateTime(2026, 6, 2, 10, 0, 0),
            SaoPauloTimeZone,
            media);

        result.IsSuccess.Should().BeTrue();
        result.Value.Media.Should().HaveCount(itemCount);
    }

    /// <summary>
    /// Rascunho editorial permite Reel sem mídia; Create completo continua exigindo vídeo 9:16.
    /// </summary>
    [Fact]
    public void CreateEditorialDraft_ReelSemMidia_DeveIniciarEmDraftComMidiaVazia()
    {
        var result = OrganicPost.CreateEditorialDraft(
            Guid.NewGuid(),
            Guid.NewGuid(),
            OrganicPlatform.Instagram,
            OrganicPostFormat.Reel,
            ContentPillar.Sale,
            "Gancho curto",
            new DateTime(2026, 6, 2, 10, 0, 0),
            SaoPauloTimeZone);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(OrganicPostStatus.Draft);
        result.Value.Media.Should().BeEmpty();
    }

    /// <summary>
    /// Valida proporção 9:16 para Reels e TikTok.
    /// </summary>
    [Theory]
    [InlineData(OrganicPostFormat.Reel)]
    [InlineData(OrganicPostFormat.TikTokVideo)]
    public void Create_VideoVerticalSemMidiaOuProporcaoInvalida_DeveRetornarMediaAspectInvalid(OrganicPostFormat format)
    {
        var withoutMedia = OrganicPost.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            OrganicPlatform.Instagram,
            format,
            ContentPillar.Sale,
            "Hook",
            new DateTime(2026, 6, 2, 10, 0, 0),
            SaoPauloTimeZone);

        withoutMedia.IsFailure.Should().BeTrue();
        withoutMedia.Error.Code.Should().Be(SocialErrors.MediaAspectInvalid.Code);

        var wrongRatio = OrganicPost.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            OrganicPlatform.Instagram,
            format,
            ContentPillar.Sale,
            "Hook",
            new DateTime(2026, 6, 2, 10, 0, 0),
            SaoPauloTimeZone,
            [(1920, 1080, 0)]);

        wrongRatio.IsFailure.Should().BeTrue();
        wrongRatio.Error.Code.Should().Be(SocialErrors.MediaAspectInvalid.Code);
    }

    /// <summary>
    /// Valida mídia 1080x1920 aceita para formato vertical.
    /// </summary>
    [Fact]
    public void Create_ReelComProporcao916_DeveTerSucesso()
    {
        var result = OrganicPost.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            OrganicPlatform.Instagram,
            OrganicPostFormat.Reel,
            ContentPillar.Authority,
            "Reel",
            new DateTime(2026, 6, 2, 10, 0, 0),
            SaoPauloTimeZone,
            [(1080, 1920, 0)]);

        result.IsSuccess.Should().BeTrue();
    }

    /// <summary>
    /// Valida transições ilegais retornam InvalidTransition.
    /// </summary>
    [Fact]
    public void TransicoesIlegais_DevemRetornarInvalidTransition()
    {
        var post = CreateDraftPost().Value;

        post.Approve().Error.Code.Should().Be(SocialErrors.InvalidTransition.Code);
        post.Schedule().Error.Code.Should().Be(SocialErrors.InvalidTransition.Code);
        post.MarkPublishing().Error.Code.Should().Be(SocialErrors.InvalidTransition.Code);
        post.MarkPublished().Error.Code.Should().Be(SocialErrors.InvalidTransition.Code);
    }

    /// <summary>
    /// Valida caminho legal de transições até Published.
    /// </summary>
    [Fact]
    public void TransicoesLegais_DevemConcluirEmPublished()
    {
        var post = CreateDraftPost().Value;

        post.SubmitForApproval().IsSuccess.Should().BeTrue();
        post.Approve().IsSuccess.Should().BeTrue();
        post.Schedule().IsSuccess.Should().BeTrue();
        post.MarkPublishing().IsSuccess.Should().BeTrue();
        post.MarkPublished().IsSuccess.Should().BeTrue();
        post.Status.Should().Be(OrganicPostStatus.Published);
    }

    /// <summary>
    /// Valida reagendamento bloqueado em Published.
    /// </summary>
    [Fact]
    public void Reschedule_EmPublished_DeveRetornarSlotNotReschedulable()
    {
        var post = CreateDraftPost().Value;
        post.SubmitForApproval().IsSuccess.Should().BeTrue();
        post.Approve().IsSuccess.Should().BeTrue();
        post.Schedule().IsSuccess.Should().BeTrue();
        post.MarkPublishing().IsSuccess.Should().BeTrue();
        post.MarkPublished().IsSuccess.Should().BeTrue();

        var reschedule = post.Reschedule(new DateTime(2026, 7, 1, 15, 0, 0), SaoPauloTimeZone);

        reschedule.IsFailure.Should().BeTrue();
        reschedule.Error.Code.Should().Be(SocialErrors.SlotNotReschedulable.Code);
    }

    /// <summary>
    /// Valida reagendamento bloqueado em Publishing e Cancelled.
    /// </summary>
    [Fact]
    public void Reschedule_EmPublishingECancelled_DeveRetornarSlotNotReschedulable()
    {
        var publishing = CreateDraftPost().Value;
        publishing.SubmitForApproval().IsSuccess.Should().BeTrue();
        publishing.Approve().IsSuccess.Should().BeTrue();
        publishing.Schedule().IsSuccess.Should().BeTrue();
        publishing.MarkPublishing().IsSuccess.Should().BeTrue();

        publishing.Reschedule(new DateTime(2026, 7, 1, 15, 0, 0), SaoPauloTimeZone)
            .Error.Code.Should().Be(SocialErrors.SlotNotReschedulable.Code);

        var cancelled = CreateDraftPost().Value;
        cancelled.Cancel().IsSuccess.Should().BeTrue();
        cancelled.Reschedule(new DateTime(2026, 7, 1, 15, 0, 0), SaoPauloTimeZone)
            .Error.Code.Should().Be(SocialErrors.SlotNotReschedulable.Code);
    }

    /// <summary>
    /// Valida conversão de horário local para UTC no fuso America/Sao_Paulo.
    /// </summary>
    [Fact]
    public void Create_DevePersistirScheduledAtUtc()
    {
        var local = new DateTime(2026, 1, 15, 10, 0, 0);
        var post = OrganicPost.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            OrganicPlatform.Instagram,
            OrganicPostFormat.FeedImage,
            ContentPillar.SocialProof,
            "Post",
            local,
            SaoPauloTimeZone).Value;

        var tz = TimeZoneInfo.FindSystemTimeZoneById(SaoPauloTimeZone);
        var expectedUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), tz);

        post.ScheduledAtUtc.Should().Be(expectedUtc);
    }

    private static Result<OrganicPost> CreateDraftPost()
    {
        return OrganicPost.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            OrganicPlatform.Instagram,
            OrganicPostFormat.FeedImage,
            ContentPillar.Authority,
            "Legenda válida",
            new DateTime(2026, 6, 2, 10, 0, 0),
            SaoPauloTimeZone);
    }
}
