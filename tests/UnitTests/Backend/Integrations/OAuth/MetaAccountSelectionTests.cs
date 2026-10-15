using BuildingBlocks.Application.Integrations;
using BuildingBlocks.Domain.Integrations;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Integrations.Application.Meta;
using Integrations.Application.Persistence;
using Integrations.Domain.Meta;
using Integrations.Domain.OAuth;
using MediatR;
using NSubstitute;

namespace UnitTests.Backend.Integrations.OAuth;

/// <summary>Garante seleção somente das contas verificadas pela autorização do workspace.</summary>
public sealed class MetaAccountSelectionTests
{
    /// <summary>ID externo arbitrário não deve chegar ao cadastro de vínculos.</summary>
    [Fact]
    public async Task Select_WithAccountNotReturnedByMeta_DoesNotRegisterAccounts()
    {
        var workspace = Guid.NewGuid();
        var vaults = Substitute.For<IOAuthTokenVaultRepository>();
        vaults.GetByWorkspaceAndPlatformAsync(workspace, "MetaAds", Arg.Any<CancellationToken>())
            .Returns(OAuthTokenVault.Create(Guid.NewGuid(), workspace, "MetaAds", "authorizer", "João", "encrypted", scopes: "ads_read").Value);
        var api = Substitute.For<IMetaAdAccountsApi>();
        api.ListAsync("token", Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<MetaAdAccount>>.Success([
            new("act_11", "Cliente A", "BRL", null, 1, ["ANALYZE"]) ]));
        var encryption = Substitute.For<IOAuthEncryptionService>();
        encryption.Decrypt("encrypted", Arg.Any<string?>()).Returns("token");
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<ValidateIntegrationWorkspaceQuery>(), Arg.Any<CancellationToken>()).Returns(Result<bool>.Success(true));
        var handler = new SelectMetaAdAccountsCommandHandler(vaults, encryption, api, sender, Substitute.For<IIntegrationsUnitOfWork>());
        var result = await handler.Handle(new(workspace, ["act_999"]), default);
        result.Error.Code.Should().Be("Meta.AccountNotAuthorized");
        await sender.DidNotReceive().Send(Arg.Any<RegisterConnectedAdAccountsCommand>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Uma autorização revogada nunca deve ser usada para consultar a Meta.</summary>
    [Fact]
    public async Task List_WithRevokedConnection_DoesNotCallMeta()
    {
        var workspace = Guid.NewGuid();
        var vault = OAuthTokenVault.Create(Guid.NewGuid(), workspace, "MetaAds", "authorizer", "João", "encrypted", scopes: "ads_read").Value;
        vault.Revoke();
        var vaults = Substitute.For<IOAuthTokenVaultRepository>();
        vaults.GetByWorkspaceAndPlatformAsync(workspace, "MetaAds", Arg.Any<CancellationToken>()).Returns(vault);
        var api = Substitute.For<IMetaAdAccountsApi>();
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<ValidateIntegrationWorkspaceQuery>(), Arg.Any<CancellationToken>()).Returns(Result<bool>.Success(true));
        var handler = new GetMetaAdAccountsQueryHandler(vaults, Substitute.For<IOAuthEncryptionService>(), api, sender, Substitute.For<IIntegrationsUnitOfWork>());
        var result = await handler.Handle(new(workspace), default);
        result.IsFailure.Should().BeTrue();
        await api.DidNotReceiveWithAnyArgs().ListAsync(default!, default);
    }
}
