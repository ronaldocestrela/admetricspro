using BuildingBlocks.Application.Emails;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Emails;
using Master.Application.Repositories;
using Master.Domain.Tenants;
using Master.Domain.Tenants.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Master.Application.Billing.Trial;

/// <summary>
/// Orquestra a avaliação contínua de inquilinos em período de testes (Trial) e despacha lembretes transacionais de 7, 3, 1 dias restantes e expiração.
/// </summary>
public sealed class TrialNotificationEngineService : ITrialNotificationEngineService
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantNotificationLogRepository _logRepository;
    private readonly IEmailSender _emailSender;
    private readonly ITransactionalEmailTemplateRenderer _templateRenderer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;
    private readonly ILogger<TrialNotificationEngineService> _logger;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TrialNotificationEngineService"/>.
    /// </summary>
    /// <param name="tenantRepository">Repositório de inquilinos.</param>
    /// <param name="logRepository">Repositório de auditoria e logs de notificação.</param>
    /// <param name="emailSender">Provedor de envio de e-mails transacionais.</param>
    /// <param name="templateRenderer">Renderizador de templates de e-mail.</param>
    /// <param name="unitOfWork">Unidade de trabalho para commit de persistência.</param>
    /// <param name="publisher">Publicador de eventos de domínio em memória.</param>
    /// <param name="logger">Logger estruturado.</param>
    public TrialNotificationEngineService(
        ITenantRepository tenantRepository,
        ITenantNotificationLogRepository logRepository,
        IEmailSender emailSender,
        ITransactionalEmailTemplateRenderer templateRenderer,
        IUnitOfWork unitOfWork,
        IPublisher publisher,
        ILogger<TrialNotificationEngineService> logger)
    {
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _logRepository = logRepository ?? throw new ArgumentNullException(nameof(logRepository));
        _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        _templateRenderer = templateRenderer ?? throw new ArgumentNullException(nameof(templateRenderer));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result<TrialNoticeExecutionSummary>> ProcessTrialNoticesCycleAsync(
        DateTime? referenceDateUtc = null,
        CancellationToken cancellationToken = default)
    {
        var referenceUtc = referenceDateUtc ?? DateTime.UtcNow;
        _logger.LogInformation("Iniciando ciclo de avaliação da régua de trial com referência {ReferenceUtc}...", referenceUtc);

        var tenants = await _tenantRepository.GetTenantsForTrialNoticeEvaluationAsync(cancellationToken);

        int evaluatedCount = tenants.Count;
        int sevenDayCount = 0;
        int threeDayCount = 0;
        int oneDayCount = 0;
        int expiredCount = 0;
        int failuresCount = 0;

        var eventsToDispatch = new List<TenantTrialNoticeSentEvent>();

        foreach (var tenant in tenants)
        {
            if (tenant.SubscriptionExpiresAtUtc is null || string.IsNullOrWhiteSpace(tenant.AdminEmail))
            {
                continue;
            }

            var sentTypes = await _logRepository.GetSentNoticeTypesForTenantAsync(tenant.Id, cancellationToken);
            var pendingNotice = TrialNoticePolicy.EvaluatePendingNotice(tenant.SubscriptionExpiresAtUtc.Value, referenceUtc, sentTypes);

            if (pendingNotice is null)
            {
                continue;
            }

            var remainingDays = Math.Max(0, (tenant.SubscriptionExpiresAtUtc.Value - referenceUtc).TotalDays);

            _logger.LogInformation(
                "Tenant {TenantId} ({Subdomain}) elegível para aviso de trial {NoticeType}. Dias restantes: {RemainingDays:F1}",
                tenant.Id.Value,
                tenant.Subdomain,
                pendingNotice.Value,
                remainingDays);

            var message = _templateRenderer.RenderTrialNoticeEmail(
                tenant.AdminEmail,
                tenant.AdminFullName ?? tenant.CompanyName,
                tenant.CompanyName,
                tenant.Subdomain,
                tenant.CustomDomain,
                pendingNotice.Value,
                tenant.SubscriptionExpiresAtUtc.Value,
                remainingDays);

            var sendResult = await _emailSender.SendEmailAsync(message, cancellationToken);

            var logResult = TenantNotificationLog.Create(
                tenant.Id,
                pendingNotice.Value,
                tenant.AdminEmail,
                message.Subject,
                sendResult.IsSuccess,
                sendResult.IsFailure ? sendResult.Error.Description : null,
                referenceUtc);

            if (logResult.IsSuccess)
            {
                await _logRepository.AddAsync(logResult.Value, cancellationToken);
            }

            if (sendResult.IsSuccess)
            {
                switch (pendingNotice.Value)
                {
                    case TrialNoticeType.TrialReminder7Days:
                        sevenDayCount++;
                        break;
                    case TrialNoticeType.TrialReminder3Days:
                        threeDayCount++;
                        break;
                    case TrialNoticeType.TrialReminder1Day:
                        oneDayCount++;
                        break;
                    case TrialNoticeType.TrialExpired:
                        expiredCount++;
                        break;
                }

                eventsToDispatch.Add(new TenantTrialNoticeSentEvent(
                    tenant.Id,
                    pendingNotice.Value,
                    remainingDays,
                    tenant.AdminEmail,
                    referenceUtc));
            }
            else
            {
                failuresCount++;
                _logger.LogWarning(
                    "Falha ao enviar e-mail de lembrete de trial ({NoticeType}) para {AdminEmail} (Tenant {Subdomain}): {ErrorDescription}",
                    pendingNotice.Value,
                    tenant.AdminEmail,
                    tenant.Subdomain,
                    sendResult.Error.Description);
            }
        }

        if (sevenDayCount > 0 || threeDayCount > 0 || oneDayCount > 0 || expiredCount > 0 || failuresCount > 0)
        {
            await _unitOfWork.CommitAsync(cancellationToken);
        }

        foreach (var domainEvent in eventsToDispatch)
        {
            await _publisher.Publish(new DomainEventNotification<TenantTrialNoticeSentEvent>(domainEvent), cancellationToken);
        }

        var summary = new TrialNoticeExecutionSummary(
            evaluatedCount,
            sevenDayCount,
            threeDayCount,
            oneDayCount,
            expiredCount,
            failuresCount,
            referenceUtc);

        _logger.LogInformation(
            "Ciclo de notificações de trial concluído. Avaliados: {Evaluated}, 7D: {SevenDay}, 3D: {ThreeDay}, 1D: {OneDay}, Expirados: {Expired}, Falhas: {Failures}",
            evaluatedCount,
            sevenDayCount,
            threeDayCount,
            oneDayCount,
            expiredCount,
            failuresCount);

        return Result<TrialNoticeExecutionSummary>.Success(summary);
    }
}
