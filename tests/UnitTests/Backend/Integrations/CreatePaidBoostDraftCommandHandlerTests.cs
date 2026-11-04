using BuildingBlocks.Domain.Integrations;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using Integrations.Application.Boost.Commands.CreatePaidBoostDraft;
using Integrations.Application.Boost.Persistence;
using Integrations.Application.Persistence;
using NSubstitute;
using SocialPublishing.Contracts.Boost;
using Xunit;

namespace UnitTests.Backend.Integrations;

/// <summary>
/// Testes do handler de rascunho pausado a partir de tração orgânica (Subfase 6.3).
/// </summary>
public sealed class CreatePaidBoostDraftCommandHandlerTests
{
    private static readonly Guid WorkspaceId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    /// <summary>
    /// Instagram produz rascunho MetaAds pausado com legenda e mídia.
    /// </summary>
    [Fact]
    public async Task Handle_Instagram_CriaRascunhoMetaAdsPausado()
    {
        var host = CreateHost();
        host.SeedMetaAccount(createdAtUtc: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var result = await host.Handler.Handle(
            new CreatePaidBoostDraftCommand(
                WorkspaceId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Instagram",
                "Legenda orgânica",
                "media/key.jpg"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        host.DraftRepository.AllDrafts.Should().ContainSingle(d =>
            d.Platform == "MetaAds" &&
            d.Status == PaidBoostDraftStatus.Paused &&
            d.Body == "Legenda orgânica" &&
            d.MediaObjectKey == "media/key.jpg" &&
            string.IsNullOrEmpty(d.ExternalAdId));
        await host.UnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Facebook Page também mapeia para MetaAds.
    /// </summary>
    [Fact]
    public async Task Handle_FacebookPage_CriaRascunhoMetaAds()
    {
        var host = CreateHost();
        host.SeedMetaAccount();

        var result = await host.Handler.Handle(
            new CreatePaidBoostDraftCommand(
                WorkspaceId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                "FacebookPage",
                "Copy",
                null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        host.DraftRepository.AllDrafts.Single().Platform.Should().Be("MetaAds");
    }

    /// <summary>
    /// TikTok orgânico produz rascunho TikTokAds.
    /// </summary>
    [Fact]
    public async Task Handle_TikTok_CriaRascunhoTikTokAds()
    {
        var host = CreateHost();
        host.SeedTikTokAccount();

        var result = await host.Handler.Handle(
            new CreatePaidBoostDraftCommand(
                WorkspaceId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                "TikTok",
                "Hook",
                "media/v.mp4"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        host.DraftRepository.AllDrafts.Single().Platform.Should().Be("TikTokAds");
    }

    /// <summary>
    /// Sem conta Meta conectada retorna Meta.AccountNotLinked.
    /// </summary>
    [Fact]
    public async Task Handle_SemContaMeta_RetornaAccountNotLinked()
    {
        var host = CreateHost();

        var result = await host.Handler.Handle(
            new CreatePaidBoostDraftCommand(
                WorkspaceId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Instagram",
                "Legenda",
                null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Meta.AccountNotLinked");
        host.DraftRepository.AllDrafts.Should().BeEmpty();
        await host.UnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Sem conta TikTok retorna SyncCampaignHierarchy.AccountNotFound.
    /// </summary>
    [Fact]
    public async Task Handle_SemContaTikTok_RetornaAccountNotFound()
    {
        var host = CreateHost();

        var result = await host.Handler.Handle(
            new CreatePaidBoostDraftCommand(
                WorkspaceId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                "TikTok",
                "Legenda",
                null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SyncCampaignHierarchy.AccountNotFound");
        host.DraftRepository.AllDrafts.Should().BeEmpty();
    }

    /// <summary>
    /// Segunda execução com o mesmo SuggestionId devolve o mesmo Guid.
    /// </summary>
    [Fact]
    public async Task Handle_IdempotentePorSuggestionId_RetornaMesmoGuid()
    {
        var host = CreateHost();
        host.SeedMetaAccount();
        var suggestionId = Guid.NewGuid();
        var command = new CreatePaidBoostDraftCommand(
            WorkspaceId,
            suggestionId,
            Guid.NewGuid(),
            "Instagram",
            "Legenda",
            null);

        var first = await host.Handler.Handle(command, CancellationToken.None);
        var second = await host.Handler.Handle(command, CancellationToken.None);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        second.Value.Should().Be(first.Value);
        host.DraftRepository.AllDrafts.Should().ContainSingle();
        await host.UnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Várias contas Meta: escolhe a mais antiga por CreatedAtUtc e desempata por Id.
    /// </summary>
    [Fact]
    public async Task Handle_VariasContasMeta_EscolheContaMaisAntiga()
    {
        var host = CreateHost();
        var olderId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var newerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        host.SeedMetaAccount(olderId, new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        host.SeedMetaAccount(newerId, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var result = await host.Handler.Handle(
            new CreatePaidBoostDraftCommand(
                WorkspaceId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Instagram",
                "Legenda",
                null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        host.DraftRepository.AllDrafts.Single().ConnectedAdAccountId.Should().Be(olderId);
    }

    private static DraftTestHost CreateHost()
    {
        var hierarchy = Substitute.For<global::Integrations.Domain.Campaigns.ICampaignHierarchyRepository>();
        var draftRepository = new InMemoryPaidBoostDraftRepository();
        var unitOfWork = Substitute.For<IIntegrationsUnitOfWork>();
        unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        hierarchy
            .GetAccountsForSyncAsync(Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ConnectedAdAccount>>(Array.Empty<ConnectedAdAccount>()));

        var clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(new DateTimeOffset(2026, 11, 3, 12, 0, 0, TimeSpan.Zero));

        var handler = new CreatePaidBoostDraftCommandHandler(
            hierarchy,
            draftRepository,
            unitOfWork,
            clock);

        return new DraftTestHost(hierarchy, draftRepository, unitOfWork, handler);
    }

    private sealed class DraftTestHost
    {
        public DraftTestHost(
            global::Integrations.Domain.Campaigns.ICampaignHierarchyRepository hierarchy,
            InMemoryPaidBoostDraftRepository draftRepository,
            IIntegrationsUnitOfWork unitOfWork,
            CreatePaidBoostDraftCommandHandler handler)
        {
            Hierarchy = hierarchy;
            DraftRepository = draftRepository;
            UnitOfWork = unitOfWork;
            Handler = handler;
        }

        public global::Integrations.Domain.Campaigns.ICampaignHierarchyRepository Hierarchy { get; }
        public InMemoryPaidBoostDraftRepository DraftRepository { get; }
        public IIntegrationsUnitOfWork UnitOfWork { get; }
        public CreatePaidBoostDraftCommandHandler Handler { get; }

        public void SeedMetaAccount(Guid? id = null, DateTime? createdAtUtc = null)
        {
            var accountId = id ?? Guid.NewGuid();
            var account = ConnectedAdAccount.Create(
                accountId,
                WorkspaceId,
                "MetaAds",
                "act_123",
                "Meta Test",
                createdAtUtc: createdAtUtc ?? DateTime.UtcNow).Value;
            if (account.Status != "Connected")
            {
                account.BindAuthorization(Guid.NewGuid(), WorkspaceId, "Meta Test", "BRL", null, 1, "ADVERTISE");
            }

            Hierarchy
                .GetAccountsForSyncAsync(WorkspaceId, null, "MetaAds", Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    var all = DraftRepository.MetaAccounts;
                    return Task.FromResult<IReadOnlyList<ConnectedAdAccount>>(all);
                });

            DraftRepository.MetaAccounts.Add(account);
        }

        public void SeedTikTokAccount()
        {
            var account = ConnectedAdAccount.Create(
                Guid.NewGuid(),
                WorkspaceId,
                "TikTokAds",
                "tt_123",
                "TikTok Test",
                createdAtUtc: DateTime.UtcNow).Value;
            account.BindAuthorization(Guid.NewGuid(), WorkspaceId, "TikTok Test", "BRL", null, 1, "ADVERTISE");
            DraftRepository.TikTokAccounts.Add(account);

            Hierarchy
                .GetAccountsForSyncAsync(WorkspaceId, null, "TikTokAds", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<ConnectedAdAccount>>(DraftRepository.TikTokAccounts));
        }
    }

    private sealed class InMemoryPaidBoostDraftRepository : IPaidBoostDraftRepository
    {
        public List<PaidBoostDraft> AllDrafts { get; } = new();
        public List<ConnectedAdAccount> MetaAccounts { get; } = new();
        public List<ConnectedAdAccount> TikTokAccounts { get; } = new();

        public Task AddAsync(PaidBoostDraft draft, CancellationToken cancellationToken = default)
        {
            AllDrafts.Add(draft);
            return Task.CompletedTask;
        }

        public Task<PaidBoostDraft?> GetBySuggestionIdAsync(
            Guid workspaceId,
            Guid suggestionId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                AllDrafts.FirstOrDefault(d => d.WorkspaceId == workspaceId && d.SuggestionId == suggestionId));
    }
}
