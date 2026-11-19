using FluentAssertions;
using SocialPublishing.Application.Planning.Copy;
using SocialPublishing.Domain.Brand;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Posts;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Application;

/// <summary>
/// Testes de <see cref="CopyBriefBuilder"/>.
/// </summary>
public sealed class CopyBriefBuilderTests
{
    /// <summary>
    /// Campos de negócio e cores preenchidos aparecem no prompt.
    /// </summary>
    [Fact]
    public void Build_ComNegocioECores_DeveIncluirNoPrompt()
    {
        var brandVoice = BrandVoiceProfile.Create(
            Guid.NewGuid(),
            "Tom",
            "Persona",
            ["https://example.com"],
            [],
            EmojiPolicy.Sparingly,
            15,
            25,
            25,
            25,
            25,
            businessSummary: "Software B2B",
            targetAudience: "PMEs de tecnologia",
            mainOffer: "Gestão de tráfego",
            differentiators: "Time certificado",
            primaryColor: "#111827",
            secondaryColor: "#F9FAFB",
            accentColor: "#10B981").Value;

        var brief = CopyBriefBuilder.Build(
            brandVoice,
            new EditorialSlotCopyInput(ContentPillar.Authority, OrganicPlatform.Instagram, OrganicPostFormat.FeedImage));

        var prompt = brief.ToPromptText();
        prompt.Should().Contain("Negócio: Software B2B");
        prompt.Should().Contain("Público-alvo: PMEs de tecnologia");
        prompt.Should().Contain("Cores da identidade visual: primária #111827");
    }

    /// <summary>
    /// Brief sem negócio nem cores não adiciona linhas extras.
    /// </summary>
    [Fact]
    public void Build_SemNegocioECores_NaoIncluiLinhasExtras()
    {
        var brandVoice = BrandVoiceProfile.Create(
            Guid.NewGuid(),
            "Tom",
            "Persona",
            ["https://example.com"],
            [],
            EmojiPolicy.Sparingly,
            15,
            25,
            25,
            25,
            25).Value;

        var brief = CopyBriefBuilder.Build(
            brandVoice,
            new EditorialSlotCopyInput(ContentPillar.Sale, OrganicPlatform.FacebookPage, OrganicPostFormat.FeedImage));

        var prompt = brief.ToPromptText();
        prompt.Should().NotContain("Negócio:");
        prompt.Should().NotContain("Cores da identidade visual:");
    }
}