using BuildingBlocks.Application.Emails;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Persistence;
using Master.Application.Emails;
using Master.Application.Repositories;
using Master.Domain.Tenants;
using Master.Domain.Tenants.Events;
using Microsoft.Extensions.Logging;

namespace Master.Application.Billing.Checkout.Events;

/// <summary>
/// Manipulador de evento de domínio disparado após ativação definitiva de assinatura paga.
/// Renderiza o recibo/confirmação em HTML, envia por e-mail e registra auditoria.
/// </summary>
public sealed class TenantSubscriptionActivatedSendConfirmationEmailHandler : IDomainEventHandler<TenantSubscriptionActivatedDomainEvent>
{
    private readonly IEmailSender _emailSender;
    private readonly ITransactionalEmailTemplateRenderer _templateRenderer;
    private readonly ITenantNotificationLogRepository _logRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TenantSubscriptionActivatedSendConfirmationEmailHandler> _logger;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TenantSubscriptionActivatedSendConfirmationEmailHandler"/>.
    /// </summary>
    /// <param name="emailSender">Provedor de envio de e-mails.</param>
    /// <param name="templateRenderer">Renderizador de templates.</param>
    /// <param name="logRepository">Repositório de logs de notificações.</param>
    /// <param name="unitOfWork">Coordenador de persistência.</param>
    /// <param name="logger">Serviço de log.</param>
    public TenantSubscriptionActivatedSendConfirmationEmailHandler(
        IEmailSender emailSender,
        ITransactionalEmailTemplateRenderer templateRenderer,
        ITenantNotificationLogRepository logRepository,
        IUnitOfWork unitOfWork,
        ILogger<TenantSubscriptionActivatedSendConfirmationEmailHandler> logger)
    {
        _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        _templateRenderer = templateRenderer ?? throw new ArgumentNullException(nameof(templateRenderer));
        _logRepository = logRepository ?? throw new ArgumentNullException(nameof(logRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task Handle(DomainEventNotification<TenantSubscriptionActivatedDomainEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        if (string.IsNullOrWhiteSpace(domainEvent.AdminEmail))
        {
            _logger.LogWarning("Assinatura do Tenant {TenantId} ativada, mas sem e-mail de administrador cadastrado.", domainEvent.TenantId.Value);
            return;
        }

        var message = _templateRenderer.RenderSubscriptionConfirmationEmail(
            domainEvent.AdminEmail,
            domainEvent.CompanyName,
            domainEvent.Tier,
            domainEvent.BillingCycle,
            domainEvent.Amount,
            domainEvent.PaidAtUtc,
            domainEvent.ExpiresAtUtc);

        var sendResult = await _emailSender.SendEmailAsync(message, cancellationToken);
        var logCreation = TenantNotificationLog.Create(
            domainEvent.TenantId,
            TrialNoticeType.SubscriptionActivated,
            domainEvent.AdminEmail,
            message.Subject,
            sendResult.IsSuccess,
            sendResult.IsFailure ? sendResult.Error.Description : null,
            domainEvent.PaidAtUtc);

        if (logCreation.IsSuccess)
        {
            await _logRepository.AddAsync(logCreation.Value, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
        }

        if (sendResult.IsFailure)
        {
            _logger.LogError("Falha no envio de e-mail de confirmação para {Email}: {Error}", domainEvent.AdminEmail, sendResult.Error.Description);
        }
        else
        {
            _logger.LogInformation("E-mail de confirmação de assinatura enviado com sucesso para {Email}.", domainEvent.AdminEmail);
        }
    }
}
