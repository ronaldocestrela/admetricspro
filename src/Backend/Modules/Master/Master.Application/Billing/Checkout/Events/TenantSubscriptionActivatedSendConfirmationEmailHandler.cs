using BuildingBlocks.Application.Messaging;
using Master.Application.Emails.Commands.SendSubscriptionConfirmationEmail;
using Master.Domain.Tenants.Events;
using MediatR;

namespace Master.Application.Billing.Checkout.Events;

/// <summary>
/// Manipulador in-process do <see cref="TenantSubscriptionActivatedDomainEvent"/> quando a mensageria está desligada.
/// </summary>
public sealed class TenantSubscriptionActivatedSendConfirmationEmailHandler
    : IDomainEventHandler<TenantSubscriptionActivatedDomainEvent>
{
    private readonly ISender _sender;
    private readonly IMessagingFeatureGate _messagingFeatureGate;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TenantSubscriptionActivatedSendConfirmationEmailHandler"/>.
    /// </summary>
    /// <param name="sender">Mediador in-process.</param>
    /// <param name="messagingFeatureGate">Feature flag de mensageria.</param>
    public TenantSubscriptionActivatedSendConfirmationEmailHandler(
        ISender sender,
        IMessagingFeatureGate messagingFeatureGate)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _messagingFeatureGate = messagingFeatureGate ?? throw new ArgumentNullException(nameof(messagingFeatureGate));
    }

    /// <inheritdoc />
    public async Task Handle(
        DomainEventNotification<TenantSubscriptionActivatedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        if (_messagingFeatureGate.IsEnabled)
        {
            return;
        }

        var domainEvent = notification.DomainEvent;
        if (string.IsNullOrWhiteSpace(domainEvent.AdminEmail))
        {
            return;
        }

        var command = new SendSubscriptionConfirmationEmailCommand(Guid.NewGuid(), domainEvent.TenantId.Value);
        await _sender.Send(command, cancellationToken);
    }
}