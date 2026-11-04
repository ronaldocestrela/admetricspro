using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using MediatR;
using NSubstitute;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Traction.Commands.AcceptPaidBoostSuggestion;
using SocialPublishing.Application.Traction.Commands.DismissPaidBoostSuggestion;
using SocialPublishing.Contracts.Boost;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Traction;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Application;

/// <summary>
/// Testes dos comandos de aceite e dispensa de sugestão de impulsionamento (Subfase 6.3).
/// </summary>
public sealed class AcceptPaidBoostSuggestionCommandTests
{
    private static readonly DateTime FixedUtc = new(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Aceite Open envia CreatePaidBoostDraftCommand uma vez e persiste Accepted com PaidDraftId.
    /// </summary>
    [Fact]
    public async Task Accept_SugestaoOpen_ComSucessoIntegrations_MarcaAcceptedEPersisteDraftId()
    {
        var host = CreateHost();
        var post = host.LifecycleHost.SeedDraftPost();
        var suggestion = SeedOpenSuggestion(host, post.Id, host.LifecycleHost.WorkspaceId);
        var draftId = Guid.NewGuid();

        host.Sender
            .Send(Arg.Any<CreatePaidBoostDraftCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Success(draftId));

        var result = await host.AcceptHandler.Handle(
            new AcceptPaidBoostSuggestionCommand(host.LifecycleHost.WorkspaceId, suggestion.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(PaidBoostSuggestionStatus.Accepted);
        result.Value.PaidDraftId.Should().Be(draftId);
        suggestion.Status.Should().Be(PaidBoostSuggestionStatus.Accepted);
        suggestion.PaidDraftId.Should().Be(draftId);

        await host.Sender.Received(1).Send(
            Arg.Is<CreatePaidBoostDraftCommand>(c =>
                c.WorkspaceId == host.LifecycleHost.WorkspaceId &&
                c.SuggestionId == suggestion.Id &&
                c.OrganicPostId == post.Id &&
                c.Network == OrganicPlatform.Instagram.ToString() &&
                c.Caption == post.Caption),
            Arg.Any<CancellationToken>());
        await host.UnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Falha do Integrations mantém Open e repassa o erro original.
    /// </summary>
    [Fact]
    public async Task Accept_FalhaIntegrations_MantemOpenERepassaErro()
    {
        var host = CreateHost();
        var post = host.LifecycleHost.SeedDraftPost();
        var suggestion = SeedOpenSuggestion(host, post.Id, host.LifecycleHost.WorkspaceId);
        var integrationError = Error.NotFound("Meta.AccountNotLinked", "Conta Meta não vinculada ativamente a este workspace.");

        host.Sender
            .Send(Arg.Any<CreatePaidBoostDraftCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Failure(integrationError));

        var result = await host.AcceptHandler.Handle(
            new AcceptPaidBoostSuggestionCommand(host.LifecycleHost.WorkspaceId, suggestion.Id),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(integrationError);
        suggestion.Status.Should().Be(PaidBoostSuggestionStatus.Open);
        suggestion.PaidDraftId.Should().BeNull();
        await host.UnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        await host.Sender.Received(1).Send(Arg.Any<CreatePaidBoostDraftCommand>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Dismiss não chama Integrations e marca Dismissed.
    /// </summary>
    [Fact]
    public async Task Dismiss_SugestaoOpen_NaoChamaIntegrations()
    {
        var host = CreateHost();
        var post = host.LifecycleHost.SeedDraftPost();
        var suggestion = SeedOpenSuggestion(host, post.Id, host.LifecycleHost.WorkspaceId);

        var result = await host.DismissHandler.Handle(
            new DismissPaidBoostSuggestionCommand(host.LifecycleHost.WorkspaceId, suggestion.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(PaidBoostSuggestionStatus.Dismissed);
        suggestion.Status.Should().Be(PaidBoostSuggestionStatus.Dismissed);
        await host.Sender.DidNotReceive().Send(Arg.Any<CreatePaidBoostDraftCommand>(), Arg.Any<CancellationToken>());
        await host.UnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Sugestão de outro workspace retorna NotFound.
    /// </summary>
    [Fact]
    public async Task Accept_OutroWorkspace_RetornaBoostSuggestionNotFound()
    {
        var host = CreateHost();
        var post = host.LifecycleHost.SeedDraftPost();
        var suggestion = SeedOpenSuggestion(host, post.Id, Guid.NewGuid());

        var result = await host.AcceptHandler.Handle(
            new AcceptPaidBoostSuggestionCommand(host.LifecycleHost.WorkspaceId, suggestion.Id),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be(SocialErrors.BoostSuggestionNotFound.Code);
        await host.Sender.DidNotReceive().Send(Arg.Any<CreatePaidBoostDraftCommand>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Sugestão inexistente retorna NotFound.
    /// </summary>
    [Fact]
    public async Task Accept_Inexistente_RetornaBoostSuggestionNotFound()
    {
        var host = CreateHost();

        var result = await host.AcceptHandler.Handle(
            new AcceptPaidBoostSuggestionCommand(host.LifecycleHost.WorkspaceId, Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.BoostSuggestionNotFound.Code);
    }

    /// <summary>
    /// Sugestão que não está Open retorna InvalidTransition.
    /// </summary>
    [Fact]
    public async Task Accept_NaoOpen_RetornaInvalidTransition()
    {
        var host = CreateHost();
        var post = host.LifecycleHost.SeedDraftPost();
        var suggestion = SeedOpenSuggestion(host, post.Id, host.LifecycleHost.WorkspaceId);
        suggestion.Accept(Guid.NewGuid()).IsSuccess.Should().BeTrue();

        var result = await host.AcceptHandler.Handle(
            new AcceptPaidBoostSuggestionCommand(host.LifecycleHost.WorkspaceId, suggestion.Id),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.InvalidTransition.Code);
        await host.Sender.DidNotReceive().Send(Arg.Any<CreatePaidBoostDraftCommand>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Post orgânico ausente no workspace retorna PostNotFound sem chamar Integrations.
    /// </summary>
    [Fact]
    public async Task Accept_PostAusente_RetornaPostNotFound()
    {
        var host = CreateHost();
        var suggestion = SeedOpenSuggestion(host, Guid.NewGuid(), host.LifecycleHost.WorkspaceId);

        var result = await host.AcceptHandler.Handle(
            new AcceptPaidBoostSuggestionCommand(host.LifecycleHost.WorkspaceId, suggestion.Id),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.PostNotFound.Code);
        await host.Sender.DidNotReceive().Send(Arg.Any<CreatePaidBoostDraftCommand>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Comando de aceite exige permissão SocialCopilotGenerate.
    /// </summary>
    [Fact]
    public void AcceptCommand_DeveExigirSocialCopilotGenerate()
    {
        OrganicPostLifecycleCommandTestHost.AssertSocialCopilotPermission<AcceptPaidBoostSuggestionCommand>();
    }

    private static PaidBoostSuggestion SeedOpenSuggestion(BoostTestHost host, Guid postId, Guid workspaceId)
    {
        var suggestion = PaidBoostSuggestion.Open(
            workspaceId,
            postId,
            OrganicPlatform.Instagram,
            impressions: 5000,
            engagementRate: 0.05m,
            baselineMedianEngagement: 0.02m,
            engagementMultiple: 2.5m,
            detectedAtUtc: FixedUtc).Value;
        host.SuggestionRepository.AllSuggestions.Add(suggestion);
        return suggestion;
    }

    private static BoostTestHost CreateHost()
    {
        var lifecycle = new OrganicPostLifecycleCommandTestHost();
        var suggestionRepository = new BoostInMemoryPaidBoostSuggestionRepository();
        var sender = Substitute.For<ISender>();
        var unitOfWork = lifecycle.UnitOfWork;
        unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var acceptHandler = new AcceptPaidBoostSuggestionCommandHandler(
            suggestionRepository,
            lifecycle.PostRepository,
            unitOfWork,
            sender);

        var dismissHandler = new DismissPaidBoostSuggestionCommandHandler(
            suggestionRepository,
            unitOfWork);

        return new BoostTestHost(lifecycle, suggestionRepository, sender, unitOfWork, acceptHandler, dismissHandler);
    }

    private sealed record BoostTestHost(
        OrganicPostLifecycleCommandTestHost LifecycleHost,
        BoostInMemoryPaidBoostSuggestionRepository SuggestionRepository,
        ISender Sender,
        ISocialPublishingUnitOfWork UnitOfWork,
        AcceptPaidBoostSuggestionCommandHandler AcceptHandler,
        DismissPaidBoostSuggestionCommandHandler DismissHandler);

    private sealed class BoostInMemoryPaidBoostSuggestionRepository : IPaidBoostSuggestionRepository
    {
        public List<PaidBoostSuggestion> AllSuggestions { get; } = new();

        public Task AddAsync(PaidBoostSuggestion suggestion, CancellationToken cancellationToken = default)
        {
            AllSuggestions.Add(suggestion);
            return Task.CompletedTask;
        }

        public Task<IReadOnlySet<Guid>> GetOpenOrganicPostIdsAsync(
            Guid workspaceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlySet<Guid>>(
                AllSuggestions
                    .Where(s => s.WorkspaceId == workspaceId && s.Status == PaidBoostSuggestionStatus.Open)
                    .Select(s => s.OrganicPostId)
                    .ToHashSet());

        public Task<PaidBoostSuggestion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(AllSuggestions.FirstOrDefault(s => s.Id == id));

        public Task<IReadOnlyList<PaidBoostSuggestion>> ListByWorkspaceAsync(
            Guid workspaceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PaidBoostSuggestion>>(
                AllSuggestions.Where(s => s.WorkspaceId == workspaceId).ToList());

        public Task UpdateAsync(PaidBoostSuggestion suggestion, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
