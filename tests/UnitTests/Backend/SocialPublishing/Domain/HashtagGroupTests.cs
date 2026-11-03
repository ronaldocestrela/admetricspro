using FluentAssertions;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Brand;
using SocialPublishing.Domain.Connections;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Domain;

/// <summary>
/// Testes unitários do value object <see cref="HashtagGroup"/>.
/// </summary>
public sealed class HashtagGroupTests
{
    /// <summary>
    /// Valida rejeição de hashtag duplicada (case insensitive, com ou sem #).
    /// </summary>
    [Fact]
    public void Create_ComDuplicata_DeveRetornarHashtagPolicyViolated()
    {
        var result = HashtagGroup.Create(
            OrganicPlatform.Instagram,
            30,
            ["#Marca", "marca", "niche"]);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.HashtagPolicyViolated.Code);
    }

    /// <summary>
    /// Valida rejeição de espaço interno na hashtag.
    /// </summary>
    [Fact]
    public void Create_ComEspacoInterno_DeveRetornarHashtagPolicyViolated()
    {
        var result = HashtagGroup.Create(
            OrganicPlatform.Instagram,
            30,
            ["tag invalida"]);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.HashtagPolicyViolated.Code);
    }

    /// <summary>
    /// Valida teto efetivo min(perfil, rede) no Instagram.
    /// </summary>
    [Fact]
    public void Create_AcimaDoTetoDoPerfilNoInstagram_DeveRetornarHashtagPolicyViolated()
    {
        var tags = Enumerable.Range(1, 11).Select(i => $"tag{i}").ToArray();

        var result = HashtagGroup.Create(OrganicPlatform.Instagram, 10, tags);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.HashtagPolicyViolated.Code);
    }

    /// <summary>
    /// Valida teto hard do TikTok (8) mesmo com perfil mais alto.
    /// </summary>
    [Fact]
    public void Create_AcimaDoTetoDoTikTok_DeveRetornarHashtagPolicyViolated()
    {
        var tags = Enumerable.Range(1, 9).Select(i => $"tag{i}").ToArray();

        var result = HashtagGroup.Create(OrganicPlatform.TikTok, 30, tags);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.HashtagPolicyViolated.Code);
    }

    /// <summary>
    /// Valida sucesso dentro do teto efetivo.
    /// </summary>
    [Fact]
    public void Create_DentroDoTeto_DeveTerSucesso()
    {
        var result = HashtagGroup.Create(
            OrganicPlatform.TikTok,
            30,
            ["marca", "produto", "dica"]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Tags.Should().HaveCount(3);
    }
}
