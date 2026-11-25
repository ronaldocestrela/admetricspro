using BuildingBlocks.Application.Messaging;
using Master.Application.Tenants.Events;
using Master.Domain.Tenants;
using Master.Domain.Tenants.Events;
using MediatR;
using NSubstitute;

namespace UnitTests.Backend.Emails;

/// <summary>
/// Testes do handler in-process de boas-vindas quando a mensageria está desligada.
/// </summary>
public sealed class TenantProvisionedSendWelcomeEmailEventHandlerTests
{
    /// <summary>
    /// Com mensageria ligada o handler não despacha comando.
    /// </summary>
    [Fact]
    public async Task Handle_WhenMessagingEnabled_ShouldNotSendCommand()
    {
        var sender = Substitute.For<ISender>();
        var gate = Substitute.For<IMessagingFeatureGate>();
        gate.IsEnabled.Returns(true);

        var handler = new TenantProvisionedSendWelcomeEmailEventHandler(sender, gate);
        var notification = new DomainEventNotification<TenantProvisionedEvent>(
            new TenantProvisionedEvent(
                TenantId.New(),
                "Co",
                "co",
                "a@b.com",
                "Admin",
                null,
                SubscriptionTier.Trial,
                DateTime.UtcNow));

        await handler.Handle(notification, CancellationToken.None);

        await sender.DidNotReceive().Send(Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Com mensageria desligada delega ao comando MediatR.
    /// </summary>
    [Fact]
    public async Task Handle_WhenMessagingDisabled_ShouldSendCommand()
    {
        var sender = Substitute.For<ISender>();
        var gate = Substitute.For<IMessagingFeatureGate>();
        gate.IsEnabled.Returns(false);

        var handler = new TenantProvisionedSendWelcomeEmailEventHandler(sender, gate);
        var tenantId = TenantId.New();
        var notification = new DomainEventNotification<TenantProvisionedEvent>(
            new TenantProvisionedEvent(
                tenantId,
                "Co",
                "co",
                "a@b.com",
                "Admin",
                null,
                SubscriptionTier.Trial,
                DateTime.UtcNow));

        await handler.Handle(notification, CancellationToken.None);

        await sender.Received(1).Send(
            Arg.Is<Master.Application.Emails.Commands.SendTenantWelcomeEmail.SendTenantWelcomeEmailCommand>(c => c.TenantId == tenantId.Value),
            Arg.Any<CancellationToken>());
    }
}