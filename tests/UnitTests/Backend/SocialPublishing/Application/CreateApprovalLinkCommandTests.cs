using FluentAssertions;
using NSubstitute;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Approvals.Commands.CreateApprovalLink;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Approvals;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Application;

/// <summary>
/// Testes de <see cref="CreateApprovalLinkCommand"/> (Subfase 5.3.1).
/// </summary>
public sealed class CreateApprovalLinkCommandTests
{
    /// <summary>
    /// Exige permissão SocialCopilotGenerate.
    /// </summary>
    [Fact]
    public void CreateApprovalLinkCommand_DeveExigirSocialCopilotGenerate()
    {
        OrganicPostLifecycleCommandTestHost.AssertSocialCopilotPermission<CreateApprovalLinkCommand>();
    }

    /// <summary>
    /// Gera token opaco de 32 bytes, persiste só SHA-256 e devolve claro uma vez.
    /// </summary>
    [Fact]
    public async Task CreateLink_DevePersistirApenasHashERetornarTokenClaro()
    {
        var host = new OrganicPostLifecycleCommandTestHost();
        var linkRepo = new InMemoryApprovalLinkRepository();
        var handler = CreateHandler(host, linkRepo);
        var expires = host.FixedTime.UtcDateTime.AddDays(7);

        var result = await handler.Handle(
            new CreateApprovalLinkCommand(host.WorkspaceId, expires),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PlainToken.Should().NotBeNullOrWhiteSpace();

        var hash = ApprovalTokenHasher.TryHashPlainToken(result.Value.PlainToken, out var bytes);
        hash.Should().NotBeNull();
        bytes!.Length.Should().Be(32);
        linkRepo.AllLinks.Should().ContainSingle();
        linkRepo.AllLinks[0].TokenHash.Should().Be(hash);
        linkRepo.AllLinks[0].TokenHash.Should().NotContain(result.Value.PlainToken);
        await host.UnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Expiração no passado não persiste link.
    /// </summary>
    [Fact]
    public async Task CreateLink_ComExpiracaoInvalida_NaoDevePersistir()
    {
        var host = new OrganicPostLifecycleCommandTestHost();
        var linkRepo = new InMemoryApprovalLinkRepository();
        var handler = CreateHandler(host, linkRepo);
        var expires = host.FixedTime.UtcDateTime.AddMinutes(-1);

        var result = await handler.Handle(
            new CreateApprovalLinkCommand(host.WorkspaceId, expires),
            CancellationToken.None);

        result.Error.Code.Should().Be(SocialErrors.ApprovalLinkExpirationInvalid.Code);
        linkRepo.AllLinks.Should().BeEmpty();
        await host.UnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Segundo link revoga o anterior do mesmo workspace.
    /// </summary>
    [Fact]
    public async Task CreateLink_SegundaEmissao_DeveRevogarAnterior()
    {
        var host = new OrganicPostLifecycleCommandTestHost();
        var linkRepo = new InMemoryApprovalLinkRepository();
        var handler = CreateHandler(host, linkRepo);
        var expires = host.FixedTime.UtcDateTime.AddDays(3);

        await handler.Handle(new CreateApprovalLinkCommand(host.WorkspaceId, expires), CancellationToken.None);
        await handler.Handle(new CreateApprovalLinkCommand(host.WorkspaceId, expires.AddDays(1)), CancellationToken.None);

        linkRepo.AllLinks.Should().HaveCount(2);
        linkRepo.AllLinks.Count(l => l.RevokedAtUtc is null).Should().Be(1);
        linkRepo.AllLinks.Single(l => l.RevokedAtUtc is null).ExpiresAtUtc.Should().Be(expires.AddDays(1));
    }

    /// <summary>
    /// Tenant ausente falha antes de tocar o repositório.
    /// </summary>
    [Fact]
    public async Task CreateLink_SemTenant_NaoDeveChamarRepositorio()
    {
        var host = new OrganicPostLifecycleCommandTestHost();
        host.TenantContextAccessor.TenantContext = BuildingBlocks.Infrastructure.MultiTenancy.TenantContext.Empty;
        var linkRepo = Substitute.For<IApprovalLinkRepository>();
        var handler = CreateHandler(host, linkRepo);

        var result = await handler.Handle(
            new CreateApprovalLinkCommand(host.WorkspaceId, host.FixedTime.UtcDateTime.AddDays(1)),
            CancellationToken.None);

        result.Error.Code.Should().Be(SocialErrors.TenantRequired.Code);
        await linkRepo.DidNotReceive().AddAsync(Arg.Any<ApprovalLink>(), Arg.Any<CancellationToken>());
    }

    private static CreateApprovalLinkCommandHandler CreateHandler(
        OrganicPostLifecycleCommandTestHost host,
        InMemoryApprovalLinkRepository linkRepo) =>
        new(
            linkRepo,
            host.UnitOfWork,
            host.TenantContextAccessor,
            host.CurrentUserContext,
            host.TimeProvider);

    private static CreateApprovalLinkCommandHandler CreateHandler(
        OrganicPostLifecycleCommandTestHost host,
        IApprovalLinkRepository linkRepo) =>
        new(
            linkRepo,
            host.UnitOfWork,
            host.TenantContextAccessor,
            host.CurrentUserContext,
            host.TimeProvider);
}
