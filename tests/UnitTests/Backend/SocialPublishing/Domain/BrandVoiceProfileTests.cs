using FluentAssertions;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Brand;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Domain;

/// <summary>
/// Testes unitários do agregado <see cref="BrandVoiceProfile"/>.
/// </summary>
public sealed class BrandVoiceProfileTests
{
    /// <summary>
    /// Valida criação com pesos válidos e metadados obrigatórios.
    /// </summary>
    [Fact]
    public void Create_ComPesosValidos_DeveCriarPerfil()
    {
        var result = BrandVoiceProfile.Create(
            Guid.NewGuid(),
            "Profissional e acolhedor",
            "Especialista em tráfego",
            ["https://example.com/sobre"],
            ["grátis", "milagre"],
            EmojiPolicy.Sparingly,
            20,
            40,
            20,
            25,
            15);

        result.IsSuccess.Should().BeTrue();
        result.Value.PillarWeights.Authority.Should().Be(40);
        result.Value.PillarWeights.Sale.Should().Be(20);
        result.Value.PillarWeights.Connection.Should().Be(25);
        result.Value.PillarWeights.SocialProof.Should().Be(15);
        result.Value.ForbiddenTerms.Should().Contain("grátis");
        result.Value.MandatoryLinks.Should().Contain("https://example.com/sobre");
    }

    /// <summary>
    /// Valida falha quando a soma dos pesos difere de 100.
    /// </summary>
    [Fact]
    public void Create_ComSomaDePesosInvalida_DeveRetornarPillarWeightsInvalid()
    {
        var result = BrandVoiceProfile.Create(
            Guid.NewGuid(),
            "Tom",
            "Persona",
            ["https://example.com"],
            [],
            EmojiPolicy.Allowed,
            15,
            30,
            30,
            30,
            30);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.PillarWeightsInvalid.Code);
    }

    /// <summary>
    /// Valida falha quando algum peso é negativo.
    /// </summary>
    [Fact]
    public void Create_ComPesoNegativo_DeveRetornarPillarWeightsInvalid()
    {
        var result = BrandVoiceProfile.Create(
            Guid.NewGuid(),
            "Tom",
            "Persona",
            ["https://example.com"],
            [],
            EmojiPolicy.Forbidden,
            10,
            -1,
            50,
            25,
            26);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.PillarWeightsInvalid.Code);
    }

    /// <summary>
    /// Valida falha quando workspace é vazio.
    /// </summary>
    [Fact]
    public void Create_ComWorkspaceVazio_DeveRetornarWorkspaceRequired()
    {
        var result = BrandVoiceProfile.Create(
            Guid.Empty,
            "Tom",
            "Persona",
            ["https://example.com"],
            [],
            EmojiPolicy.Allowed,
            10,
            25,
            25,
            25,
            25);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.WorkspaceRequired.Code);
    }
}
