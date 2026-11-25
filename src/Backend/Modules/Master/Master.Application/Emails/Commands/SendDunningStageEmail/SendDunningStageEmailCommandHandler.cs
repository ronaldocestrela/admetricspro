using BuildingBlocks.Application.Emails;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Emails;
using Master.Application.Messaging;
using Master.Application.Repositories;
using Master.Domain.Tenants;
using Microsoft.Extensions.Logging;

namespace Master.Application.Emails.Commands.SendDunningStageEmail;

/// <summary>
/// Envia e-mail de marco de inadimplência com idempotência por episódio de cobrança.
/// </summary>
public sealed class SendDunningStageEmailCommandHandler : ICommandHandler<SendDunningStageEmailCommand>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IEmailSender _emailSender;
    private readonly ITransactionalEmailTemplateRenderer _templateRenderer;
    private readonly ITenantNotificationLogRepository _logRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SendDunningStageEmailCommandHandler> _logger;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="SendDunningStageEmailCommandHandler"/>.
    /// </summary>
    /// <param name="tenantRepository">Repositório de tenants.</param>
    /// <param name="emailSender">Provedor SMTP.</param>
    /// <param name="templateRenderer">Renderizador de templates.</param>
    /// <param name="logRepository">Auditoria de notificações.</param>
    /// <param name="unitOfWork">Unidade de trabalho Master.</param>
    /// <param name="logger">Logger estruturado.</param>
    public SendDunningStageEmailCommandHandler(
        ITenantRepository tenantRepository,
        IEmailSender emailSender,
        ITransactionalEmailTemplateRenderer templateRenderer,
        ITenantNotificationLogRepository logRepository,
        IUnitOfWork unitOfWork,
        ILogger<SendDunningStageEmailCommandHandler> logger)
    {
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        _templateRenderer = templateRenderer ?? throw new ArgumentNullException(nameof(templateRenderer));
        _logRepository = logRepository ?? throw new ArgumentNullException(nameof(logRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result> Handle(SendDunningStageEmailCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.PaymentDueDateUtc.Kind != DateTimeKind.Utc)
        {
            return Result.Failure(Error.Validation("Dunning.DueDateNotUtc", "PaymentDueDateUtc deve estar em UTC."));
        }

        var noticeType = DunningEmailNoticeMapper.ToNoticeType(command.DunningStage);
        if (noticeType is null)
        {
            return Result.Failure(Error.Validation("Dunning.StageNotNotifiable", "Estágio não gera e-mail transacional."));
        }

        var tenantId = new TenantId(command.TenantId);
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            return Result.Failure(Error.NotFound("Tenant.NotFound", "Tenant não localizado no catálogo."));
        }

        if (string.IsNullOrWhiteSpace(tenant.AdminEmail))
        {
            _logger.LogWarning(
                "Tenant {TenantId} sem e-mail de administrador; aviso de dunning {Stage} ignorado.",
                tenant.Id.Value,
                command.DunningStage);
            return Result.Success();
        }

        var alreadySent = await _logRepository.HasSuccessfulNoticeSinceAsync(
            tenant.Id,
            noticeType.Value,
            command.PaymentDueDateUtc,
            cancellationToken);

        if (alreadySent)
        {
            _logger.LogInformation(
                "Aviso de dunning {NoticeType} já enviado para o episódio {DueDate} do tenant {TenantId}.",
                noticeType.Value,
                command.PaymentDueDateUtc,
                tenant.Id.Value);
            return Result.Success();
        }

        var daysOverdue = DunningPolicy.CalculateDaysOverdue(command.PaymentDueDateUtc, DateTime.UtcNow);

        var emailMessage = _templateRenderer.RenderDunningStageEmail(
            tenant.AdminEmail,
            tenant.AdminFullName ?? tenant.CompanyName,
            tenant.CompanyName,
            tenant.Subdomain,
            tenant.CustomDomain,
            command.DunningStage,
            command.PaymentDueDateUtc,
            daysOverdue);

        var sendResult = await _emailSender.SendEmailAsync(emailMessage, cancellationToken);
        var logResult = TenantNotificationLog.Create(
            tenant.Id,
            noticeType.Value,
            tenant.AdminEmail,
            emailMessage.Subject,
            sendResult.IsSuccess,
            sendResult.IsFailure ? sendResult.Error.Description : null,
            DateTime.UtcNow);

        if (logResult.IsSuccess)
        {
            await _logRepository.AddAsync(logResult.Value, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
        }

        if (sendResult.IsFailure)
        {
            _logger.LogWarning(
                "Falha ao enviar aviso de dunning {Stage} para {Email}: {Error}",
                command.DunningStage,
                tenant.AdminEmail,
                sendResult.Error.Description);
            return Result.Failure(sendResult.Error);
        }

        return Result.Success();
    }
}