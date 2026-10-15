using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Application.Security;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Integrations.Application.OAuth.Commands.HandleOAuthCallback;
using Integrations.Application.Persistence;
using Integrations.Domain.OAuth;
using MediatR;
using NSubstitute;

namespace UnitTests.Backend.Integrations.OAuth;

/// <summary>Garante que callbacks não troquem tokens entre usuários ou tenants.</summary>
public sealed class OAuthCallbackSecurityTests
{
    /// <summary>Contexto divergente nunca pode chegar à troca do código.</summary>
    [Theory]
    [InlineData("tenant")]
    [InlineData("user")]
    [InlineData("redirect")]
    [InlineData("replay")]
    public async Task Callback_WithMismatchedContext_DoesNotExchangeCode(string mismatch)
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var context = Substitute.For<ITenantContextAccessor>();
        var tenant = Substitute.For<ITenantContext>();
        tenant.TenantId.Returns(tenantId);
        context.TenantContext.Returns(tenant);
        var user = Substitute.For<ICurrentUserContext>();
        user.UserId.Returns(userId);
        user.IsAuthenticated.Returns(true);
        var state = Substitute.For<IOAuthStateService>();
        state.ValidateAndUnpackState("state").Returns(Result<OAuthStatePayload>.Success(new(
            mismatch == "tenant" ? Guid.NewGuid() : tenantId, Guid.NewGuid(), "MetaAds",
            "https://app.example/integrations", DateTime.UtcNow,
            mismatch == "user" ? Guid.NewGuid() : userId, "nonce")));
        var vaults = Substitute.For<IOAuthTokenVaultRepository>();
        vaults.ConsumeStateAsync("state", Arg.Any<CancellationToken>()).Returns(mismatch != "replay");
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<BuildingBlocks.Application.Integrations.ValidateIntegrationWorkspaceQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<bool>.Success(true));
        var auth = Substitute.For<IAdNetworkAuthService>();
        var handler = new HandleOAuthCallbackCommandHandler(state, auth, vaults,
            Substitute.For<IOAuthEncryptionService>(), Substitute.For<IIntegrationsUnitOfWork>(), context, user, sender);
        var result = await handler.Handle(new("code", "state",
            mismatch == "redirect" ? "https://attacker.example" : "https://app.example/integrations"), default);
        result.IsFailure.Should().BeTrue();
        await auth.DidNotReceiveWithAnyArgs().ExchangeCodeAsync(default!, default!, default!, default);
    }
}
