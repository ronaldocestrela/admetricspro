using FluentAssertions;
using SocialPublishing.Application.Planning.Images;
using SocialPublishing.Domain.Brand;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Posts;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Application;

/// <summary>
/// Testes de montagem do brief de imagem social.
/// </summary>
public sealed class ImageBriefBuilderTests
{
    private const string SaoPauloTimeZone = "America/Sao_Paulo";

    /// <summary>
    /// Brief do Instagram deve pedir proporção 4:5 e incluir legenda sem hashtags.
    /// </summary>
    [Fact]
    public void Build_Instagram_DeveUsarProporcao45ESemHashtags()
    {
        var brandVoice = SeedBrandVoice(primaryColor: "#112233");
        var post = SeedApprovedPost(OrganicPlatform.Instagram, "Corpo do post\n#marca #nicho");

        var brief = ImageBriefBuilder.Build(brandVoice, post, "1K", null);
        var prompt = brief.ToPromptText();

        brief.AspectRatio.Should().Be(ImageBriefBuilder.InstagramFeedAspectRatio);
        brief.CaptionBody.Should().Be("Corpo do post");
        prompt.Should().Contain("Corpo do post");
        prompt.Should().Contain("primária #112233");
        prompt.Should().NotContain("#marca");
    }

    /// <summary>
    /// Brief não deve inventar oferta ausente.
    /// </summary>
    [Fact]
    public void Build_SemOferta_NaoDeveInventarOfertaNoPrompt()
    {
        var brandVoice = SeedBrandVoice(mainOffer: null);
        var post = SeedApprovedPost(OrganicPlatform.FacebookPage, "Texto");

        var prompt = ImageBriefBuilder.Build(brandVoice, post, "1K", null).ToPromptText();

        prompt.Should().NotContain("Oferta principal:");

        var brief = ImageBriefBuilder.Build(brandVoice, post, "1K", null);
        brief.AspectRatio.Should().Be(ImageBriefBuilder.FacebookFeedAspectRatio);
    }

    private static BrandVoiceProfile SeedBrandVoice(
        string? primaryColor = null,
        string? mainOffer = "Oferta X")
    {
        var result = BrandVoiceProfile.Create(
            Guid.NewGuid(),
            "Tom",
            "Persona",
            [],
            [],
            EmojiPolicy.Sparingly,
            hashtagLimit: 20,
            authorityWeight: 40,
            saleWeight: 20,
            connectionWeight: 25,
            socialProofWeight: 15,
            languageCode: "pt-BR",
            businessSummary: "Resumo",
            targetAudience: "Público",
            mainOffer: mainOffer,
            differentiators: null,
            primaryColor: primaryColor,
            secondaryColor: null,
            accentColor: null);

        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    private static OrganicPost SeedApprovedPost(OrganicPlatform platform, string caption)
    {
        var create = OrganicPost.CreateEditorialDraft(
            Guid.NewGuid(),
            Guid.Empty,
            platform,
            OrganicPostFormat.FeedImage,
            ContentPillar.Authority,
            caption,
            new DateTime(2026, 11, 10, 10, 0, 0),
            SaoPauloTimeZone);

        create.IsSuccess.Should().BeTrue();
        var post = create.Value;
        post.SubmitForApproval().IsSuccess.Should().BeTrue();
        post.Approve().IsSuccess.Should().BeTrue();
        return post;
    }
}