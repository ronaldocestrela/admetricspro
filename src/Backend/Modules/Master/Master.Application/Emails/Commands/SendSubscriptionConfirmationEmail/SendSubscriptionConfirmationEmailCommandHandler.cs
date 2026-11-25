using BuildingBlocks.Application.Emails;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Emails;
using Master.Application.Repositories;
using Master.Domain.Billing;
using Master.Domain.Tenants;
using Microsoft.Extensions.Logging;

namespace Master.Application.Emails.Commands.SendSubscriptionConfirmationEmail;

/// <summary>
/// Envia confirmação de assinatura com idempotência via <see cref="TenantNotificationLog"/>.
/// </summary>
public sealed class SendSubscriptionConfirmationEmailCommandHandler : ICommandHandler<SendSubscriptionConfirmationEmailCommand>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantPaymentTransactionRepository _transactionRepository;
    private readonly IEmailSender _emailSender;
    private readonly ITransactionalEmailTemplateRenderer _templateRenderer;
    private readonly ITenantNotificationLogRepository _logRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SendSubscriptionConfirmationEmailCommandHandler> _logger;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="SendSubscriptionConfirmationEmailCommandHandler"/>.
    /// </summary>
    public SendSubscriptionConfirmationEmailCommandHandler(
        ITenantRepository tenantRepository,
        ITenantPaymentTransactionRepository transactionRepository,
        IEmailSender emailSender,
        ITransactionalEmailTemplateRenderer templateRenderer,
        ITenantNotificationLogRepository logRepository,
        IUnitOfWork unitOfWork,
        ILogger<SendSubscriptionConfirmationEmailCommandHandler> logger)
    {
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        _templateRenderer = templateRenderer ?? throw new ArgumentNullException(nameof(templateRenderer));
        _logRepository = logRepository ?? throw new ArgumentNullException(nameof(logRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result> Handle(SendSubscriptionConfirmationEmailCommand command, CancellationToken cancellationToken)
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
            _logger.LogWarning("Tenant {TenantId} sem e-mail de administrador; confirmação ignorada.", tenant.Id.Value);
            return Result.Success();
        }

        var sentTypes = await _logRepository.GetSentNoticeTypesForTenantAsync(tenant.Id, cancellationToken);
        if (sentTypes.Contains(TrialNoticeType.SubscriptionActivated))
        {
            _logger.LogInformation(
                "Confirmação de assinatura já enviada para tenant {TenantId}; idempotência aplicada.",
                tenant.Id.Value);
            return Result.Success();
        }

        var transactions = await _transactionRepository.GetByTenantIdAsync(tenant.Id, cancellationToken);
        var paidTransaction = transactions.FirstOrDefault(t => t.Status == PaymentTransactionStatus.Paid);
        var paidAtUtc = paidTransaction?.PaidAtUtc ?? DateTime.UtcNow;
        var amount = paidTransaction?.Amount ?? 0m;
        var billingCycle = tenant.BillingCycle ?? paidTransaction?.BillingCycle ?? "Monthly";
        var expiresAtUtc = tenant.SubscriptionExpiresAtUtc ?? paidAtUtc.AddDays(30);

        var message = _templateRenderer.RenderSubscriptionConfirmationEmail(
            tenant.AdminEmail,
            tenant.CompanyName,
            tenant.Tier,
            billingCycle,
            amount,
            paidAtUtc,
            expiresAtUtc);

        var sendResult = await _emailSender.SendEmailAsync(message, cancellationToken);
        var logResult = TenantNotificationLog.Create(
            tenant.Id,
            TrialNoticeType.SubscriptionActivated,
            tenant.AdminEmail,
            message.Subject,
            sendResult.IsSuccess,
            sendResult.IsFailure ? sendResult.Error.Description : null,
            paidAtUtc);

        if (logResult.IsSuccess)
        {
            await _logRepository.AddAsync(logResult.Value, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
        }

        if (sendResult.IsFailure)
        {
            _logger.LogError(
                "Falha no envio de confirmação para {Email}: {Error}",
                tenant.AdminEmail,
                sendResult.Error.Description);
            return Result.Failure(sendResult.Error);
        }

        return Result.Success();
    }
}