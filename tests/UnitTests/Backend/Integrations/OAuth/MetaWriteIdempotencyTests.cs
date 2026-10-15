using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingBlocks.Application.Integrations;
using BuildingBlocks.Application.Security;
using BuildingBlocks.Domain.Integrations;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using Integrations.Application.Meta;
using Integrations.Application.Persistence;
using Integrations.Domain.Campaigns;
using Integrations.Domain.Meta;
using Integrations.Domain.OAuth;
using MediatR;
using NSubstitute;

namespace UnitTests.Backend.Integrations.OAuth;

/// <summary>Idempotência impede POST duplicado após sucesso ou resultado incerto.</summary>
public sealed class MetaWriteIdempotencyTests
{
    /// <summary>Uma operação já reservada nunca pode provocar novo POST.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Handle_ReplayedOperation_DoesNotWriteAgain(bool succeeded)
    {
        var workspace = Guid.NewGuid(); var accountId = Guid.NewGuid(); var operationId = Guid.NewGuid(); var userId = Guid.NewGuid();
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<ValidateIntegrationWorkspaceQuery>(), Arg.Any<CancellationToken>()).Returns(Result<bool>.Success(true));
        var user = Substitute.For<ICurrentUserContext>(); user.UserId.Returns(userId);
        var account = ConnectedAdAccount.Create(accountId, workspace, "MetaAds", "act_11", "Cliente").Value;
        var vault = OAuthTokenVault.Create(Guid.NewGuid(), workspace, "MetaAds", "user", "João", "encrypted", scopes: "ads_read,ads_management").Value;
        account.BindAuthorization(vault.Id, workspace, "Cliente", "BRL", null, 1, "MANAGE");
        var accounts = Substitute.For<ICampaignHierarchyRepository>();
        accounts.GetAccountsForSyncAsync(workspace, accountId, "MetaAds", Arg.Any<CancellationToken>()).Returns(new[] { account });
        var vaults = Substitute.For<IOAuthTokenVaultRepository>();
        vaults.GetByWorkspaceAndPlatformAsync(workspace, "MetaAds", Arg.Any<CancellationToken>()).Returns(vault);
        var encryption = Substitute.For<IOAuthEncryptionService>(); encryption.Decrypt("encrypted", Arg.Any<string?>()).Returns("token");
        var journal = Substitute.For<IMetaOperationRepository>();
        var api = Substitute.For<IMetaManagementApi>();
        var data = JsonSerializer.SerializeToElement(new { name = "Novo nome" });
        var command = new WriteMetaResourceCommand(operationId, workspace, accountId, "campaigns", "123", data);
        var validated = MetaWriteValidation.Validate(command.Resource, command.ExternalId, data, false).Value;
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            { command.WorkspaceId, command.AccountId, command.Resource, command.ExternalId, Data = validated }))));
        var previous = MetaApiOperation.Create(operationId, workspace, accountId, userId, "campaigns", hash).Value;
        if (succeeded) previous.Complete("123", null);
        journal.TryBeginAsync(Arg.Any<MetaApiOperation>(), Arg.Any<CancellationToken>()).Returns(Result<bool>.Success(false));
        journal.GetAsync(operationId, workspace, accountId, Arg.Any<CancellationToken>()).Returns(previous);
        var handler = new WriteMetaResourceCommandHandler(sender, user, accounts, vaults, encryption, api, journal, Substitute.For<IIntegrationsUnitOfWork>());
        var result = await handler.Handle(command, default);
        result.IsSuccess.Should().Be(succeeded);
        api.ReceivedCalls().Should().BeEmpty();
    }
}
