using FluentAssertions;
using NSubstitute;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Approvals;
using SocialPublishing.Application.Approvals.Commands.DecidePublicApproval;
using SocialPublishing.Application.Approvals.Queries.GetPublicApprovalBoard;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Approvals;
using SocialPublishing.Domain.Posts;
using Tenants.Application.Branding.Queries.GetTenantBranding;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Application;

/// <summary>
/// Testes de quadro e decisão pública (Subfase 5.3.2).
/// </summary>
public sealed class PublicApprovalQueryAndCommandTests
{
    /// <summary>
    /// Handler do quadro não referencia branding de tenant.
    /// </summary>
    [Fact]
    public void GetPublicApprovalBoardQueryHandler_NaoDeveDependerDeTenantBranding()
    {
        var ctor = typeof(GetPublicApprovalBoardQueryHandler).GetConstructors().Single();
        ctor.GetParameters().Select(p => p.ParameterType).Should().NotContain(typeof(GetTenantBrandingQuery));
    }

    /// <summary>
    /// Token válido retorna posts PendingApproval do workspace do link.
    /// </summary>
    [Fact]
    public async Task GetBoard_ComTokenValido_RetornaPostsPendingApproval()
    {
        var host = new OrganicPostLifecycleCommandTestHost();
        var linkRepo = new InMemoryApprovalLinkRepository();
        var plainToken = ApprovalTokenHasher.GeneratePlainToken(out var bytes);
        var hash = ApprovalTokenHasher.HashBytes(bytes);
        var expires = host.FixedTime.UtcDateTime.AddDays(2);
        linkRepo.AllLinks.Add(ApprovalLink.Create(host.WorkspaceId, hash, expires, host.FixedTime.UtcDateTime).Value);

        var pending = host.SeedDraftPost();
        OrganicPostLifecycleCommandTestHost.AdvanceTo(pending, OrganicPostStatus.PendingApproval);
        var approved = host.SeedDraftPost();
        OrganicPostLifecycleCommandTestHost.AdvanceTo(approved, OrganicPostStatus.Approved);

        var handler = new GetPublicApprovalBoardQueryHandler(
            linkRepo,
            host.PostRepository,
            host.TenantContextAccessor,
            host.TimeProvider);

        var result = await handler.Handle(new GetPublicApprovalBoardQuery(plainToken), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Posts.Should().ContainSingle();
        result.Value.Posts[0].Id.Should().Be(pending.Id);
    }

    /// <summary>
    /// Token inválido, expirado e revogado retornam o mesmo erro.
    /// </summary>
    [Theory]
    [InlineData("token-invalido")]
    public async Task GetBoard_TokenInvalido_RetornaApprovalTokenInvalid(string token)
    {
        var host = new OrganicPostLifecycleCommandTestHost();
        var linkRepo = new InMemoryApprovalLinkRepository();
        var handler = new GetPublicApprovalBoardQueryHandler(
            linkRepo,
            host.PostRepository,
            host.TenantContextAccessor,
            host.TimeProvider);

        var result = await handler.Handle(new GetPublicApprovalBoardQuery(token), CancellationToken.None);
        result.Error.Code.Should().Be(SocialErrors.ApprovalTokenInvalid.Code);
    }

    /// <summary>
    /// Link expirado retorna o mesmo código de token inválido.
    /// </summary>
    [Fact]
    public async Task GetBoard_LinkExpirado_RetornaApprovalTokenInvalid()
    {
        var host = new OrganicPostLifecycleCommandTestHost();
        var linkRepo = new InMemoryApprovalLinkRepository();
        var plainToken = ApprovalTokenHasher.GeneratePlainToken(out var bytes);
        var hash = ApprovalTokenHasher.HashBytes(bytes);
        var expires = host.FixedTime.UtcDateTime.AddMinutes(-5);
        linkRepo.AllLinks.Add(ApprovalLink.Create(host.WorkspaceId, hash, expires, host.FixedTime.UtcDateTime.AddDays(-2)).Value);

        var handler = new GetPublicApprovalBoardQueryHandler(
            linkRepo,
            host.PostRepository,
            host.TenantContextAccessor,
            host.TimeProvider);

        var result = await handler.Handle(new GetPublicApprovalBoardQuery(plainToken), CancellationToken.None);
        result.Error.Code.Should().Be(SocialErrors.ApprovalTokenInvalid.Code);
    }

    /// <summary>
    /// Approve move posts selecionados para Approved.
    /// </summary>
    [Fact]
    public async Task Decide_Approve_MoveParaApproved()
    {
        var (handler, host, linkRepo, plainToken, post) = await SeedDecideScenarioAsync();

        var result = await handler.Handle(
            new DecidePublicApprovalCommand(
                plainToken,
                new[] { post.Id },
                PublicApprovalDecision.Approve,
                null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        post.Status.Should().Be(OrganicPostStatus.Approved);
        await host.UnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        linkRepo.AllLinks.Should().NotBeEmpty();
    }

    /// <summary>
    /// RequestChanges grava comentário e status ChangesRequested.
    /// </summary>
    [Fact]
    public async Task Decide_RequestChanges_GravaComentarioEStatus()
    {
        var (handler, host, _, plainToken, post) = await SeedDecideScenarioAsync();

        var result = await handler.Handle(
            new DecidePublicApprovalCommand(
                plainToken,
                new[] { post.Id },
                PublicApprovalDecision.RequestChanges,
                "Ajustar CTA"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        post.Status.Should().Be(OrganicPostStatus.ChangesRequested);
        post.ClientApprovalComment.Should().Be("Ajustar CTA");
    }

    /// <summary>
    /// Post de outro workspace falha.
    /// </summary>
    [Fact]
    public async Task Decide_PostDeOutroWorkspace_Falha()
    {
        var (handler, _, _, plainToken, _) = await SeedDecideScenarioAsync();
        var otherPost = new OrganicPostLifecycleCommandTestHost().SeedDraftPost();
        OrganicPostLifecycleCommandTestHost.AdvanceTo(otherPost, OrganicPostStatus.PendingApproval);

        var result = await handler.Handle(
            new DecidePublicApprovalCommand(
                plainToken,
                new[] { otherPost.Id },
                PublicApprovalDecision.Approve,
                null),
            CancellationToken.None);

        result.Error.Code.Should().Be(SocialErrors.PostNotFound.Code);
    }

    /// <summary>
    /// Tenant ausente falha antes do repositório de link.
    /// </summary>
    [Fact]
    public async Task GetBoard_SemTenant_NaoConsultaRepositorio()
    {
        var host = new OrganicPostLifecycleCommandTestHost();
        host.TenantContextAccessor.TenantContext = BuildingBlocks.Infrastructure.MultiTenancy.TenantContext.Empty;
        var linkRepo = Substitute.For<IApprovalLinkRepository>();
        var handler = new GetPublicApprovalBoardQueryHandler(
            linkRepo,
            host.PostRepository,
            host.TenantContextAccessor,
            host.TimeProvider);

        var result = await handler.Handle(new GetPublicApprovalBoardQuery("abc"), CancellationToken.None);
        result.Error.Code.Should().Be(SocialErrors.TenantRequired.Code);
        await linkRepo.DidNotReceive().GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private static async Task<(
        DecidePublicApprovalCommandHandler Handler,
        OrganicPostLifecycleCommandTestHost Host,
        InMemoryApprovalLinkRepository LinkRepo,
        string PlainToken,
        OrganicPost Post)> SeedDecideScenarioAsync()
    {
        var host = new OrganicPostLifecycleCommandTestHost();
        var linkRepo = new InMemoryApprovalLinkRepository();
        var plainToken = ApprovalTokenHasher.GeneratePlainToken(out var bytes);
        var hash = ApprovalTokenHasher.HashBytes(bytes);
        var expires = host.FixedTime.UtcDateTime.AddDays(2);
        linkRepo.AllLinks.Add(ApprovalLink.Create(host.WorkspaceId, hash, expires, host.FixedTime.UtcDateTime).Value);

        var post = host.SeedDraftPost();
        OrganicPostLifecycleCommandTestHost.AdvanceTo(post, OrganicPostStatus.PendingApproval);

        var handler = new DecidePublicApprovalCommandHandler(
            linkRepo,
            host.PostRepository,
            host.UnitOfWork,
            host.TenantContextAccessor,
            host.TimeProvider);

        await Task.CompletedTask;
        return (handler, host, linkRepo, plainToken, post);
    }
}
