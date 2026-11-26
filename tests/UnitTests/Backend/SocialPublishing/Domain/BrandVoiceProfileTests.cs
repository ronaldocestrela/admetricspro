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

    /// <summary>
    /// Valida criação com contexto de negócio e cores opcionais preenchidos.
    /// </summary>
    [Fact]
    public void Create_ComNegocioECoresValidos_DevePersistirCampos()
    {
        var result = BrandVoiceProfile.Create(
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
            businessSummary: "Clínica odontológica premium",
            targetAudience: "Adultos 30-55 em capitais",
            mainOffer: "Clareamento e implantes",
            differentiators: "Atendimento 24h",
            primaryColor: "#2563EB",
            secondaryColor: "#fff",
            accentColor: "#FF5722");

        result.IsSuccess.Should().BeTrue();
        result.Value.BusinessProfile!.BusinessSummary.Should().Be("Clínica odontológica premium");
        result.Value.VisualIdentity!.PrimaryColor.Should().Be("#2563EB");
        result.Value.VisualIdentity.SecondaryColor.Should().Be("#fff");
    }

    /// <summary>
    /// Valida falha quando cor hexadecimal é inválida.
    /// </summary>
    [Fact]
    public void Create_ComCorInvalida_DeveRetornarBrandColorInvalid()
    {
        var result = BrandVoiceProfile.Create(
            Guid.NewGuid(),
            "Tom",
            "Persona",
            ["https://example.com"],
            [],
            EmojiPolicy.Allowed,
            10,
            25,
            25,
            25,
            25,
            primaryColor: "azul");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.BrandColorInvalid.Code);
    }

    /// <summary>
    /// Valida falha quando resumo do negócio excede o limite.
    /// </summary>
    [Fact]
    public void Create_ComResumoLongo_DeveRetornarBrandBusinessInfoTooLong()
    {
        var result = BrandVoiceProfile.Create(
            Guid.NewGuid(),
            "Tom",
            "Persona",
            ["https://example.com"],
            [],
            EmojiPolicy.Allowed,
            10,
            25,
            25,
            25,
            25,
            businessSummary: new string('a', BrandBusinessProfile.MaxBusinessSummaryLength + 1));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.BrandBusinessInfoTooLong.Code);
    }

    /// <summary>
    /// Revise atualiza negócio e cores mantendo identidade do agregado.
    /// </summary>
    [Fact]
    public void Revise_ComNegocioECores_DeveAtualizarSemAlterarId()
    {
        var created = BrandVoiceProfile.Create(
            Guid.NewGuid(),
            "Tom",
            "Persona",
            ["https://example.com"],
            [],
            EmojiPolicy.Sparingly,
            10,
            25,
            25,
            25,
            25).Value;

        var id = created.Id;

        var result = created.Revise(
            "Tom",
            "Persona",
            ["https://example.com"],
            [],
            EmojiPolicy.Sparingly,
            10,
            25,
            25,
            25,
            25,
            businessSummary: "E-commerce de moda",
            primaryColor: "#1E40AF");

        result.IsSuccess.Should().BeTrue();
        created.Id.Should().Be(id);
        created.BusinessProfile!.BusinessSummary.Should().Be("E-commerce de moda");
        created.VisualIdentity!.PrimaryColor.Should().Be("#1E40AF");
    }

    /// <summary>
    /// AttachLogo persiste chave e MIME válidos.
    /// </summary>
    [Fact]
    public void AttachLogo_ComDadosValidos_DevePersistir()
    {
        var created = BrandVoiceProfile.Create(
            Guid.NewGuid(),
            "Tom",
            "Persona",
            ["https://example.com"],
            [],
            EmojiPolicy.Sparingly,
            10,
            25,
            25,
            25,
            25).Value;

        var result = created.AttachLogo("organic-media/brand-voice/abc/logo", "image/png");

        result.IsSuccess.Should().BeTrue();
        created.LogoObjectKey.Should().Be("organic-media/brand-voice/abc/logo");
        created.LogoContentType.Should().Be("image/png");
        created.HasLogo.Should().BeTrue();
    }

    /// <summary>
    /// AttachLogo rejeita chave ou MIME ausentes.
    /// </summary>
    [Fact]
    public void AttachLogo_ComMetadadosInvalidos_DeveFalhar()
    {
        var created = BrandVoiceProfile.Create(
            Guid.NewGuid(),
            "Tom",
            "Persona",
            ["https://example.com"],
            [],
            EmojiPolicy.Sparingly,
            10,
            25,
            25,
            25,
            25).Value;

        created.AttachLogo(string.Empty, "image/png").IsFailure.Should().BeTrue();
        created.AttachLogo("key", string.Empty).IsFailure.Should().BeTrue();
        created.HasLogo.Should().BeFalse();
    }

    /// <summary>
    /// ClearLogo remove metadados de logo.
    /// </summary>
    [Fact]
    public void ClearLogo_ComLogoAnexada_DeveZerar()
    {
        var created = BrandVoiceProfile.Create(
            Guid.NewGuid(),
            "Tom",
            "Persona",
            ["https://example.com"],
            [],
            EmojiPolicy.Sparingly,
            10,
            25,
            25,
            25,
            25).Value;

        created.AttachLogo("key", "image/jpeg").IsSuccess.Should().BeTrue();
        var result = created.ClearLogo();

        result.IsSuccess.Should().BeTrue();
        created.LogoObjectKey.Should().BeNull();
        created.LogoContentType.Should().BeNull();
        created.HasLogo.Should().BeFalse();
    }

    /// <summary>
    /// Revise não remove a logo já anexada.
    /// </summary>
    [Fact]
    public void Revise_ComLogoAnexada_DevePreservarLogo()
    {
        var created = BrandVoiceProfile.Create(
            Guid.NewGuid(),
            "Tom",
            "Persona",
            ["https://example.com"],
            [],
            EmojiPolicy.Sparingly,
            10,
            25,
            25,
            25,
            25).Value;

        created.AttachLogo("logo-key", "image/webp").IsSuccess.Should().BeTrue();

        var result = created.Revise(
            "Tom novo",
            "Persona nova",
            ["https://example.com"],
            [],
            EmojiPolicy.Sparingly,
            10,
            25,
            25,
            25,
            25);

        result.IsSuccess.Should().BeTrue();
        created.Tone.Should().Be("Tom novo");
        created.LogoObjectKey.Should().Be("logo-key");
        created.LogoContentType.Should().Be("image/webp");
    }
}
