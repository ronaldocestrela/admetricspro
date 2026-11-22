using FluentAssertions;
using WebApp.Theme;
using Xunit;

namespace UnitTests.Frontend.Theme;

/// <summary>
/// Testes unitários para <see cref="ThemePreferenceResolver"/>.
/// </summary>
public sealed class ThemePreferenceResolverTests
{
    /// <summary>
    /// Valida que preferência salva &quot;light&quot; tem prioridade sobre o sistema.
    /// </summary>
    [Fact]
    public void Resolve_WhenSavedLight_ShouldReturnLightRegardlessOfSystem()
    {
        ThemePreferenceResolver.Resolve("light", systemPrefersDark: true).Should().Be("light");
        ThemePreferenceResolver.Resolve("light", systemPrefersDark: false).Should().Be("light");
        ThemePreferenceResolver.Resolve("light", systemPrefersDark: null).Should().Be("light");
    }

    /// <summary>
    /// Valida que preferência salva &quot;dark&quot; tem prioridade sobre o sistema.
    /// </summary>
    [Fact]
    public void Resolve_WhenSavedDark_ShouldReturnDarkRegardlessOfSystem()
    {
        ThemePreferenceResolver.Resolve("dark", systemPrefersDark: true).Should().Be("dark");
        ThemePreferenceResolver.Resolve("dark", systemPrefersDark: false).Should().Be("dark");
    }

    /// <summary>
    /// Valida fallback para o esquema de cores do sistema quando não há preferência salva válida.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid")]
    public void Resolve_WhenNoValidSaved_ShouldUseSystemPreference(string? saved)
    {
        ThemePreferenceResolver.Resolve(saved, systemPrefersDark: true).Should().Be("dark");
        ThemePreferenceResolver.Resolve(saved, systemPrefersDark: false).Should().Be("light");
    }

    /// <summary>
    /// Valida que o padrão do produto é tema claro quando não há preferência salva nem sinal do sistema.
    /// </summary>
    [Fact]
    public void Resolve_WhenNoSavedAndNoSystem_ShouldDefaultToLight()
    {
        ThemePreferenceResolver.Resolve(null, systemPrefersDark: null).Should().Be("light");
    }
}