using FluentAssertions;
using SocialPublishing.Application.Posts;
using SocialPublishing.Domain.Brand;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Posts;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Application;

/// <summary>
/// Testes de projeção de <see cref="OrganicPost"/> para DTOs de leitura.
/// </summary>
public sealed class OrganicPostMappingTests
{
    private const string SaoPauloTimeZone = "America/Sao_Paulo";

    /// <summary>
    /// Comentário do portal público deve ser exposto no DTO de listagem/detalhe.
    /// </summary>
    [Fact]
    public void ToDto_ComClientApprovalComment_DeveProjetarComentario()
    {
        var post = SeedPendingPost();
        post.RequestChanges("Ajustar CTA").IsSuccess.Should().BeTrue();

        var dto = OrganicPostMapping.ToDto(post);

        dto.ClientApprovalComment.Should().Be("Ajustar CTA");
        dto.Status.Should().Be(OrganicPostStatus.ChangesRequested);
    }

    /// <summary>
    /// Post sem comentário de aprovação deve projetar <c>null</c>.
    /// </summary>
    [Fact]
    public void ToDto_SemClientApprovalComment_DeveProjetarNull()
    {
        var post = SeedPendingPost();

        var dto = OrganicPostMapping.ToDto(post);

        dto.ClientApprovalComment.Should().BeNull();
    }

    private static OrganicPost SeedPendingPost()
    {
        var create = OrganicPost.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            OrganicPlatform.Instagram,
            OrganicPostFormat.FeedImage,
            ContentPillar.Authority,
            "Legenda",
            new DateTime(2026, 11, 10, 10, 0, 0),
            SaoPauloTimeZone);

        create.IsSuccess.Should().BeTrue();
        var post = create.Value;
        post.SubmitForApproval().IsSuccess.Should().BeTrue();
        return post;
    }
}