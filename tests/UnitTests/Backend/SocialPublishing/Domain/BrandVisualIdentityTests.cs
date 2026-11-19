using FluentAssertions;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Brand;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Domain;

/// <summary>
/// Testes unitários de <see cref="BrandVisualIdentity"/>.
/// </summary>
public sealed class BrandVisualIdentityTests
{
    /// <summary>
    /// Cores vazias retornam sucesso com valor nulo.
    /// </summary>
    [Fact]
    public void Create_SemCores_DeveRetornarNull()
    {
        var result = BrandVisualIdentity.Create(null, " ", null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    /// <summary>
    /// Hex inválido retorna BrandColorInvalid.
    /// </summary>
    [Fact]
    public void Create_ComHexInvalido_DeveFalhar()
    {
        var result = BrandVisualIdentity.Create("not-a-color", null, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.BrandColorInvalid.Code);
    }
}