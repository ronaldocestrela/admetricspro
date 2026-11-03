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

    /// <summary>
    /// Revise mantém identidade e carimbo de criação e atualiza metadados.
    /// </summary>
    [Fact]
    public void Revise_ComDadosValidos_DeveManterIdECreatedAtUtc()
    {
        var created = BrandVoiceProfile.Create(
            Guid.NewGuid(),
            "Tom original",
            "Persona original",
            ["https://example.com/old"],
            ["antigo"],
            EmojiPolicy.Allowed,
            10,
            25,
            25,
            25,
            25).Value;

        var id = created.Id;
        var createdAt = created.CreatedAtUtc;

        var result = created.Revise(
            "Tom revisado",
            "Persona revisada",
            ["https://example.com/new"],
            ["novo"],
            EmojiPolicy.Forbidden,
            18,
            50,
            10,
            20,
            20,
            "en-US");

        result.IsSuccess.Should().BeTrue();
        created.Id.Should().Be(id);
        created.CreatedAtUtc.Should().Be(createdAt);
        created.Tone.Should().Be("Tom revisado");
        created.Persona.Should().Be("Persona revisada");
        created.MandatoryLinks.Should().ContainSingle().Which.Should().Be("https://example.com/new");
        created.ForbiddenTerms.Should().Contain("novo");
        created.EmojiPolicy.Should().Be(EmojiPolicy.Forbidden);
        created.HashtagLimit.Should().Be(18);
        created.LanguageCode.Should().Be("en-US");
        created.PillarWeights.Authority.Should().Be(50);
    }

    /// <summary>
    /// Revise com pesos inválidos não altera o estado do agregado.
    /// </summary>
    [Fact]
    public void Revise_ComPesosInvalidos_NaoDeveAlterarEstado()
    {
        var created = BrandVoiceProfile.Create(
            Guid.NewGuid(),
            "Tom",
            "Persona",
            ["https://example.com"],
            [],
            EmojiPolicy.Sparingly,
            10,
            40,
            20,
            25,
            15).Value;

        var toneBefore = created.Tone;

        var result = created.Revise(
            "Outro tom",
            "Outra persona",
            ["https://example.com"],
            [],
            EmojiPolicy.Allowed,
            10,
            10,
            10,
            10,
            10,
            "pt-BR");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.PillarWeightsInvalid.Code);
        created.Tone.Should().Be(toneBefore);
        created.PillarWeights.Authority.Should().Be(40);
    }
}
