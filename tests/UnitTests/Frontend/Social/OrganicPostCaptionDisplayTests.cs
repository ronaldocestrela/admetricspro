using FluentAssertions;
using WebApp.Components.Social;
using Xunit;

namespace UnitTests.Frontend.Social;

/// <summary>
/// Testes unitários de <see cref="OrganicPostCaptionDisplay"/>.
/// </summary>
public sealed class OrganicPostCaptionDisplayTests
{
    /// <summary>
    /// Sequências literais <c>\n</c> viram quebra real.
    /// </summary>
    [Fact]
    public void NormalizeLiteralEscapes_WithBackslashN_ProducesNewline()
    {
        var result = OrganicPostCaptionDisplay.NormalizeLiteralEscapes("A\\nB");
        result.Should().Be("A\nB");
    }

    /// <summary>
    /// Linha final só com hashtags é separada do corpo.
    /// </summary>
    [Fact]
    public void Parse_WithHashtagLine_SplitsBodyAndTags()
    {
        var parts = OrganicPostCaptionDisplay.Parse("Gancho\\n\\n#Marca #Nicho");
        parts.Body.Should().Be("Gancho");
        parts.Hashtags.Should().BeEquivalentTo(["#Marca", "#Nicho"]);
    }

    /// <summary>
    /// Linhas finais duplicadas só com hashtags não repetem chips na UI.
    /// </summary>
    [Fact]
    public void Parse_WithDuplicateHashtagLines_DeduplicatesTagsPreservingOrder()
    {
        const string tags = "#LavaJato #EsteticaAutomotiva #GestaoSimples";
        var caption = $"Gancho\n{tags}\n{tags}";
        var parts = OrganicPostCaptionDisplay.Parse(caption);

        parts.Body.Should().Be("Gancho");
        parts.Hashtags.Should().Equal(["#LavaJato", "#EsteticaAutomotiva", "#GestaoSimples"]);
    }
}