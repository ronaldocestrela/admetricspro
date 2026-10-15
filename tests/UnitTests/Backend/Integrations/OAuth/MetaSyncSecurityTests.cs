using BuildingBlocks.Domain.Integrations;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using Integrations.Application.Campaigns.Commands.SyncCampaignHierarchy;
using Integrations.Application.Persistence;
using Integrations.Domain.Campaigns;
using Integrations.Domain.Campaigns.Sync;
using Integrations.Domain.OAuth;
using MediatR;
using NSubstitute;

namespace UnitTests.Backend.Integrations.OAuth;

/// <summary>Jobs e comandos não podem usar credenciais de outro vínculo ou workspace.</summary>
public sealed class MetaSyncSecurityTests
{
    /// <summary>Mesmo com IDs externos iguais, a credencial deve corresponder ao vínculo interno.</summary>
    [Theory]
    [InlineData("connection")]
    [InlineData("workspace")]
    [InlineData("revoked")]
    [InlineData("disconnected")]
    public async Task Sync_WithInvalidBinding_DoesNotCallExternalAdapter(string mismatch)
    {
        var workspaceId = Guid.NewGuid();
        var account = ConnectedAdAccount.Create(Guid.NewGuid(), workspaceId, "MetaAds", "act_11", "Cliente").Value;
        var vault = OAuthTokenVault.Create(Guid.NewGuid(), workspaceId, "MetaAds", "user", "João", "encrypted").Value;
        account.BindAuthorization(mismatch == "connection" ? Guid.NewGuid() : vault.Id, workspaceId, "Cliente", "BRL", null, 1, "ANALYZE");
        if (mismatch == "revoked") vault.Revoke();
        if (mismatch == "disconnected") account.Disconnect();
        var requestedWorkspace = mismatch == "workspace" ? Guid.NewGuid() : workspaceId;
        var repository = Substitute.For<ICampaignHierarchyRepository>();
        repository.GetAccountsForSyncAsync(requestedWorkspace, account.Id, null, Arg.Any<CancellationToken>()).Returns(new[] { account });
        var vaults = Substitute.For<IOAuthTokenVaultRepository>();
        vaults.GetByWorkspaceAndPlatformAsync(workspaceId, "MetaAds", Arg.Any<CancellationToken>()).Returns(vault);
        var dispatcher = Substitute.For<ICampaignHierarchySyncDispatcher>();
        var handler = new SyncCampaignHierarchyCommandHandler(repository, vaults,
            Substitute.For<IOAuthEncryptionService>(), dispatcher, Substitute.For<IIntegrationsUnitOfWork>(), Substitute.For<IPublisher>(),
            Substitute.For<BuildingBlocks.Application.MultiTenancy.ITenantContextAccessor>());
        var result = await handler.Handle(new(requestedWorkspace, account.Id), default);
        result.IsFailure.Should().BeTrue();
        dispatcher.ReceivedCalls().Should().BeEmpty();
    }
}
