using BuildingBlocks.Application.Emails;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Emails;
using Master.Application.Repositories;
using Master.Domain.Tenants;
using Microsoft.Extensions.Logging;

namespace Master.Application.Emails.Commands.SendTenantWelcomeEmail;

/// <summary>
/// Envia o e-mail de boas-vindas com idempotência via <see cref="TenantNotificationLog"/>.
/// </summary>
public sealed class SendTenantWelcomeEmailCommandHandler : ICommandHandler<SendTenantWelcomeEmailCommand>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IEmailSender _emailSender;
    private readonly ITransactionalEmailTemplateRenderer _templateRenderer;
    private readonly ITenantNotificationLogRepository _logRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SendTenantWelcomeEmailCommandHandler> _logger;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="SendTenantWelcomeEmailCommandHandler"/>.
    /// </summary>
    /// <param name="tenantRepository">Repositório de tenants.</param>
    /// <param name="emailSender">Provedor SMTP.</param>
    /// <param name="templateRenderer">Renderizador de templates.</param>
    /// <param name="logRepository">Auditoria de notificações.</param>
    /// <param name="unitOfWork">Unidade de trabalho Master.</param>
    /// <param name="logger">Logger estruturado.</param>
    public SendTenantWelcomeEmailCommandHandler(
        ITenantRepository tenantRepository,
        IEmailSender emailSender,
        ITransactionalEmailTemplateRenderer templateRenderer,
        ITenantNotificationLogRepository logRepository,
        IUnitOfWork unitOfWork,
        ILogger<SendTenantWelcomeEmailCommandHandler> logger)
    {
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        _templateRenderer = templateRenderer ?? throw new ArgumentNullException(nameof(templateRenderer));
        _logRepository = logRepository ?? throw new ArgumentNullException(nameof(logRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result> Handle(SendTenantWelcomeEmailCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var tenantId = new TenantId(command.TenantId);
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            return Result.Failure(Error.NotFound("Tenant.NotFound", "Tenant não localizado no catálogo."));
        }

        if (string.IsNullOrWhiteSpace(tenant.AdminEmail))
        {
            _logger.LogWarning(
                "Tenant {TenantId} sem e-mail de administrador; boas-vindas ignorado.",
                tenant.Id.Value);
            return Result.Success();
        }

        var sentTypes = await _logRepository.GetSentNoticeTypesForTenantAsync(tenant.Id, cancellationToken);
        if (sentTypes.Contains(TrialNoticeType.WelcomeEmail))
        {
            _logger.LogInformation(
                "Boas-vindas já enviado para tenant {TenantId}; idempotência aplicada.",
                tenant.Id.Value);
            return Result.Success();
        }

        var emailMessage = _templateRenderer.RenderWelcomeEmail(
            tenant.AdminEmail,
            tenant.AdminFullName ?? tenant.CompanyName,
            tenant.CompanyName,
            tenant.Subdomain,
            tenant.CustomDomain,
            tenant.Tier);

        var sendResult = await _emailSender.SendEmailAsync(emailMessage, cancellationToken);
        var logResult = TenantNotificationLog.Create(
            tenant.Id,
            TrialNoticeType.WelcomeEmail,
            tenant.AdminEmail,
            emailMessage.Subject,
            sendResult.IsSuccess,
            sendResult.IsFailure ? sendResult.Error.Description : null);

        if (logResult.IsSuccess)
        {
            await _logRepository.AddAsync(logResult.Value, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
        }

        if (sendResult.IsFailure)
        {
            _logger.LogWarning(
                "Falha ao enviar boas-vindas para {Email}: {Error}",
                tenant.AdminEmail,
                sendResult.Error.Description);
            return Result.Failure(sendResult.Error);
        }

        return Result.Success();
    }
}