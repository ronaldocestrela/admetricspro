using BuildingBlocks.Application.Emails;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Persistence;
using Master.Application.Emails;
using Master.Application.Repositories;
using Master.Domain.Tenants;
using Master.Domain.Tenants.Events;
using Microsoft.Extensions.Logging;

namespace Master.Application.Tenants.Events;

/// <summary>
/// Manipulador desacoplado do evento de domínio <see cref="TenantProvisionedEvent"/>.
/// Responsável por renderizar o template de boas-vindas, enviar a notificação via e-mail e registrar a auditoria transacional.
/// </summary>
public sealed class TenantProvisionedSendWelcomeEmailEventHandler : IDomainEventHandler<TenantProvisionedEvent>
{
    private readonly IEmailSender _emailSender;
    private readonly ITransactionalEmailTemplateRenderer _templateRenderer;
    private readonly ITenantNotificationLogRepository _logRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TenantProvisionedSendWelcomeEmailEventHandler> _logger;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TenantProvisionedSendWelcomeEmailEventHandler"/>.
    /// </summary>
    /// <param name="emailSender">Provedor de despacho de e-mails.</param>
    /// <param name="templateRenderer">Renderizador de templates de e-mail.</param>
    /// <param name="logRepository">Repositório de logs de notificações.</param>
    /// <param name="unitOfWork">Unidade de trabalho para commit de persistência.</param>
    /// <param name="logger">Logger estruturado.</param>
    public TenantProvisionedSendWelcomeEmailEventHandler(
        IEmailSender emailSender,
        ITransactionalEmailTemplateRenderer templateRenderer,
        ITenantNotificationLogRepository logRepository,
        IUnitOfWork unitOfWork,
        ILogger<TenantProvisionedSendWelcomeEmailEventHandler> logger)
    {
        _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        _templateRenderer = templateRenderer ?? throw new ArgumentNullException(nameof(templateRenderer));
        _logRepository = logRepository ?? throw new ArgumentNullException(nameof(logRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task Handle(DomainEventNotification<TenantProvisionedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        if (string.IsNullOrWhiteSpace(domainEvent.AdminEmail))
        {
            _logger.LogWarning(
                "Tenant {TenantId} ({Subdomain}) provisionado sem e-mail de administrador. O e-mail de boas-vindas não foi enviado.",
                domainEvent.TenantId.Value,
                domainEvent.Subdomain);
            return;
        }

        _logger.LogInformation(
            "Renderizando e-mail de boas-vindas para o tenant {TenantId} ({Subdomain}) destinado a {AdminEmail}...",
            domainEvent.TenantId.Value,
            domainEvent.Subdomain,
            domainEvent.AdminEmail);

        var emailMessage = _templateRenderer.RenderWelcomeEmail(
            domainEvent.AdminEmail,
            domainEvent.AdminFullName,
            domainEvent.CompanyName,
            domainEvent.Subdomain,
            domainEvent.CustomDomain,
            domainEvent.Tier);

        var sendResult = await _emailSender.SendEmailAsync(emailMessage, cancellationToken);

        var logResult = TenantNotificationLog.Create(
            domainEvent.TenantId,
            TrialNoticeType.WelcomeEmail,
            domainEvent.AdminEmail,
            emailMessage.Subject,
            sendResult.IsSuccess,
            sendResult.IsFailure ? sendResult.Error.Description : null);

        if (logResult.IsSuccess)
        {
            await _logRepository.AddAsync(logResult.Value, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
        }

        if (sendResult.IsSuccess)
        {
            _logger.LogInformation(
                "E-mail de boas-vindas para {AdminEmail} (Tenant {Subdomain}) enviado com sucesso.",
                domainEvent.AdminEmail,
                domainEvent.Subdomain);
        }
        else
        {
            _logger.LogWarning(
                "Falha ao enviar e-mail de boas-vindas para {AdminEmail} (Tenant {Subdomain}): {ErrorDescription}",
                domainEvent.AdminEmail,
                domainEvent.Subdomain,
                sendResult.Error.Description);
        }
    }
}
