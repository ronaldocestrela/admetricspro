using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Campaigns.Events;
using BuildingBlocks.Domain.Integrations;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using Integrations.Application.Campaigns.Commands.SyncCampaignHierarchy;
using Integrations.Application.Persistence;
using Integrations.Domain.Campaigns;
using Integrations.Domain.Campaigns.Models;
using Integrations.Domain.Campaigns.Sync;
using Integrations.Domain.OAuth;
using MediatR;
using NSubstitute;

namespace UnitTests.Backend.Integrations.Campaigns;

/// <summary>
/// Testes unitários para o orquestrador <see cref="SyncCampaignHierarchyCommandHandler"/>.
/// </summary>
public sealed class SyncCampaignHierarchyCommandHandlerTests
{
    private readonly ICampaignHierarchyRepository _hierarchyRepository = Substitute.For<ICampaignHierarchyRepository>();
    private readonly IOAuthTokenVaultRepository _tokenVaultRepository = Substitute.For<IOAuthTokenVaultRepository>();
    private readonly IOAuthEncryptionService _encryptionService = Substitute.For<IOAuthEncryptionService>();
    private readonly ICampaignHierarchySyncDispatcher _syncDispatcher = Substitute.For<ICampaignHierarchySyncDispatcher>();
    private readonly IIntegrationsUnitOfWork _unitOfWork = Substitute.For<IIntegrationsUnitOfWork>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly ITenantContextAccessor _tenantContextAccessor = Substitute.For<ITenantContextAccessor>();

    private readonly SyncCampaignHierarchyCommandHandler _handler;

    /// <summary>
    /// Inicializa dependências mockadas e o handler sob teste.
    /// </summary>
    public SyncCampaignHierarchyCommandHandlerTests()
    {
        _handler = new SyncCampaignHierarchyCommandHandler(
            _hierarchyRepository,
            _tokenVaultRepository,
            _encryptionService,
            _syncDispatcher,
            _unitOfWork,
            _publisher,
            _tenantContextAccessor);
    }

    /// <summary>
    /// Valida que requisição com WorkspaceId vazio retorna erro de validação.
    /// </summary>
    [Fact]
    public async Task Handle_WhenWorkspaceIdEmpty_ShouldReturnValidationError()
    {
        // Arrange
        var command = new SyncCampaignHierarchyCommand(Guid.Empty);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SyncCampaignHierarchy.EmptyWorkspaceId");
    }

    /// <summary>
    /// Valida que quando uma conta específica solicitada não existe, o handler retorna NotFound.
    /// </summary>
    [Fact]
    public async Task Handle_WhenSpecificAccountNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var command = new SyncCampaignHierarchyCommand(workspaceId, accountId);

        _hierarchyRepository.GetAccountsForSyncAsync(workspaceId, accountId, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ConnectedAdAccount>>(Array.Empty<ConnectedAdAccount>()));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SyncCampaignHierarchy.AccountNotFound");
    }

    /// <summary>
    /// Valida que para conta demo, a sincronização conclui com sucesso, persiste no banco e emite o evento in-memory.
    /// </summary>
    [Fact]
    public async Task Handle_WhenAccountIsDemo_ShouldSyncPersistAndEmitDomainEvent()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var demoAccount = ConnectedAdAccount.Create(
            accountId,
            workspaceId,
            "MetaAds",
            "demo_123",
            "Conta Demonstração Meta",
            "BRL",
            isDemo: true).Value;

        _hierarchyRepository.GetAccountsForSyncAsync(workspaceId, accountId, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ConnectedAdAccount>>(new[] { demoAccount }));

        var sampleHierarchy = new UnifiedCampaignHierarchy(
            new List<UnifiedCampaignItem> { new("cmp_1", "Cmp 1", BuildingBlocks.Domain.Campaigns.CampaignStatus.Active, "SALES", 100m, null, "BRL", null, null) },
            new List<UnifiedAdSetItem> { new("set_1", "cmp_1", "Set 1", BuildingBlocks.Domain.Campaigns.AdSetStatus.Active, null, null, 100m, null, null, null, null) },
            new List<UnifiedAdItem> { new("ad_1", "set_1", "cmp_1", "Ad 1", BuildingBlocks.Domain.Campaigns.AdStatus.Active, BuildingBlocks.Domain.Campaigns.AdCreativeType.Image, null, null, null, null, null) });

        _syncDispatcher.DispatchAsync(demoAccount, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<UnifiedCampaignHierarchy>.Success(sampleHierarchy)));

        _hierarchyRepository.UpsertHierarchyBatchAsync(
                workspaceId,
                accountId,
                "MetaAds",
                sampleHierarchy,
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<int>.Success(3)));

        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        var command = new SyncCampaignHierarchyCommand(workspaceId, accountId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalAccountsProcessed.Should().Be(1);
        result.Value.TotalCampaignsSynced.Should().Be(1);
        result.Value.TotalAdSetsSynced.Should().Be(1);
        result.Value.TotalAdsSynced.Should().Be(1);

        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _publisher.Received(1).Publish(
            Arg.Is<DomainEventNotification<CampaignHierarchySyncedEvent>>(
                n => n.DomainEvent.Platform == "MetaAds" &&
                     n.DomainEvent.CampaignsSynced == 1 &&
                     n.DomainEvent.WorkspaceId == workspaceId),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que para conta real sem credenciais ativas no vault, retorna erro de não autorizado.
    /// </summary>
    [Fact]
    public async Task Handle_WhenRealAccountHasNoVaultCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var realAccount = ConnectedAdAccount.Create(
            accountId,
            workspaceId,
            "MetaAds",
            "act_9988",
            "Conta Real Sem Token",
            "BRL",
            isDemo: false).Value;

        _hierarchyRepository.GetAccountsForSyncAsync(workspaceId, accountId, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ConnectedAdAccount>>(new[] { realAccount }));

        _tokenVaultRepository.GetByWorkspaceAndPlatformAsync(workspaceId, "MetaAds", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<OAuthTokenVault?>(null));

        var command = new SyncCampaignHierarchyCommand(workspaceId, accountId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SyncCampaignHierarchy.CredentialsNotFound");
    }
}
