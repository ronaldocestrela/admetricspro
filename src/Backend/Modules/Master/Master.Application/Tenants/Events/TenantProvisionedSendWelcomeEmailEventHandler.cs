using BuildingBlocks.Application.Messaging;
using Master.Application.Emails.Commands.SendTenantWelcomeEmail;
using Master.Domain.Tenants.Events;
using MediatR;

namespace Master.Application.Tenants.Events;

/// <summary>
/// Manipulador in-process do <see cref="TenantProvisionedEvent"/> quando a mensageria está desligada.
/// </summary>
public sealed class TenantProvisionedSendWelcomeEmailEventHandler : IDomainEventHandler<TenantProvisionedEvent>
{
    private readonly ISender _sender;
    private readonly IMessagingFeatureGate _messagingFeatureGate;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TenantProvisionedSendWelcomeEmailEventHandler"/>.
    /// </summary>
    /// <param name="sender">Mediador in-process.</param>
    /// <param name="messagingFeatureGate">Feature flag de mensageria durável.</param>
    public TenantProvisionedSendWelcomeEmailEventHandler(
        ISender sender,
        IMessagingFeatureGate messagingFeatureGate)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _messagingFeatureGate = messagingFeatureGate ?? throw new ArgumentNullException(nameof(messagingFeatureGate));
    }

    /// <inheritdoc />
    public async Task Handle(DomainEventNotification<TenantProvisionedEvent> notification, CancellationToken cancellationToken)
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

        var command = new SendTenantWelcomeEmailCommand(Guid.NewGuid(), domainEvent.TenantId.Value);
        await _sender.Send(command, cancellationToken);
    }
}