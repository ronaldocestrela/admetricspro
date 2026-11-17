using FluentAssertions;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Posts;
using SocialPublishing.Infrastructure.Media;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Infrastructure;

/// <summary>
/// Testes do leitor de cabeçalho de mídia orgânica.
/// </summary>
public sealed class OrganicMediaHeaderInspectorTests
{
    /// <summary>
    /// PNG 1×1 mínimo deve retornar dimensões corretas.
    /// </summary>
    [Fact]
    public async Task ReadAsync_Png1x1_DeveRetornarDimensoes()
    {
        var inspector = new OrganicMediaHeaderInspector();
        var png = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

        await using var stream = new MemoryStream(png);
        var result = await inspector.ReadAsync(stream, OrganicMediaMimeTypes.Png, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Width.Should().Be(1);
        result.Value.Height.Should().Be(1);
        result.Value.DurationMilliseconds.Should().BeNull();
    }

    /// <summary>
    /// Conteúdo inválido retorna MediaUnreadable.
    /// </summary>
    [Fact]
    public async Task ReadAsync_JpegInvalido_DeveRetornarMediaUnreadable()
    {
        var inspector = new OrganicMediaHeaderInspector();
        await using var stream = new MemoryStream([0, 1, 2, 3]);

        var result = await inspector.ReadAsync(stream, OrganicMediaMimeTypes.Jpeg, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.MediaUnreadable.Code);
    }
}
