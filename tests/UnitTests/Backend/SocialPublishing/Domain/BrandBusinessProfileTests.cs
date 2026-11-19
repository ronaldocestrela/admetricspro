using FluentAssertions;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Brand;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Domain;

/// <summary>
/// Testes unitários de <see cref="BrandBusinessProfile"/>.
/// </summary>
public sealed class BrandBusinessProfileTests
{
    /// <summary>
    /// Campos vazios retornam sucesso com valor nulo.
    /// </summary>
    [Fact]
    public void Create_ComCamposVazios_DeveRetornarNull()
    {
        var result = BrandBusinessProfile.Create(null, "  ", "", "\t");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }
}