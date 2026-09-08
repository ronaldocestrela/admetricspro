using BuildingBlocks.Application.Emails;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Emails;
using Master.Application.Repositories;
using Master.Domain.Tenants;
using Microsoft.Extensions.Logging;

namespace Master.Application.Tenants.Commands.ResendWelcomeEmail;

/// <summary>
/// Manipulador do comando <see cref="ResendWelcomeEmailCommand"/>.
/// </summary>
public sealed class ResendWelcomeEmailCommandHandler : ICommandHandler<ResendWelcomeEmailCommand, bool>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IEmailSender _emailSender;
    private readonly ITransactionalEmailTemplateRenderer _templateRenderer;
    private readonly ITenantNotificationLogRepository _logRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ResendWelcomeEmailCommandHandler> _logger;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ResendWelcomeEmailCommandHandler"/>.
    /// </summary>
    /// <param name="tenantRepository">Repositório de inquilinos.</param>
    /// <param name="emailSender">Provedor de envio de e-mails.</param>
    /// <param name="templateRenderer">Renderizador de templates.</param>
    /// <param name="logRepository">Repositório de logs de notificação.</param>
    /// <param name="unitOfWork">Unidade de trabalho.</param>
    /// <param name="logger">Logger estruturado.</param>
    public ResendWelcomeEmailCommandHandler(
        ITenantRepository tenantRepository,
        IEmailSender emailSender,
        ITransactionalEmailTemplateRenderer templateRenderer,
        ITenantNotificationLogRepository logRepository,
        IUnitOfWork unitOfWork,
        ILogger<ResendWelcomeEmailCommandHandler> logger)
    {
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        _templateRenderer = templateRenderer ?? throw new ArgumentNullException(nameof(templateRenderer));
        _logRepository = logRepository ?? throw new ArgumentNullException(nameof(logRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(ResendWelcomeEmailCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var tenantId = new TenantId(command.TenantId);
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);

        if (tenant is null)
        {
            return Result<bool>.Failure(
                Error.NotFound("Tenant.NotFound", $"Inquilino com ID {command.TenantId} não foi encontrado."));
        }

        if (string.IsNullOrWhiteSpace(tenant.AdminEmail))
        {
            return Result<bool>.Failure(
                Error.Validation("Tenant.AdminEmailMissing", "O inquilino não possui e-mail de administrador cadastrado no catálogo."));
        }

        var message = _templateRenderer.RenderWelcomeEmail(
            tenant.AdminEmail,
            tenant.AdminFullName ?? tenant.CompanyName,
            tenant.CompanyName,
            tenant.Subdomain,
            tenant.CustomDomain,
            tenant.Tier);

        var sendResult = await _emailSender.SendEmailAsync(message, cancellationToken);

        var logResult = TenantNotificationLog.Create(
            tenant.Id,
            TrialNoticeType.WelcomeEmail,
            tenant.AdminEmail,
            message.Subject,
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
                "Falha ao reenviar e-mail de boas-vindas para {AdminEmail} (Tenant {Subdomain}): {ErrorDescription}",
                tenant.AdminEmail,
                tenant.Subdomain,
                sendResult.Error.Description);

            return Result<bool>.Failure(sendResult.Error);
        }

        _logger.LogInformation(
            "E-mail de boas-vindas reenviado com sucesso para {AdminEmail} (Tenant {Subdomain}).",
            tenant.AdminEmail,
            tenant.Subdomain);

        return Result<bool>.Success(true);
    }
}
