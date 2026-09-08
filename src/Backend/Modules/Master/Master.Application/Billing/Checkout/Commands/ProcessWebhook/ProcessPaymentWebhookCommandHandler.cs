using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Billing.Payments;
using Master.Application.Repositories;
using Master.Domain.Billing;
using Microsoft.Extensions.Options;

namespace Master.Application.Billing.Checkout.Commands.ProcessWebhook;

/// <summary>
/// Manipulador responsável pelo processamento assíncrono e idempotente de webhooks emitidos por gateways de pagamento.
/// </summary>
public sealed class ProcessPaymentWebhookCommandHandler : ICommandHandler<ProcessPaymentWebhookCommand, PaymentWebhookResult>
{
    private readonly ITenantPaymentTransactionRepository _transactionRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PaymentWebhookOptions _webhookOptions;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ProcessPaymentWebhookCommandHandler"/>.
    /// </summary>
    /// <param name="transactionRepository">Repositório de transações financeiras.</param>
    /// <param name="tenantRepository">Repositório de inquilinos.</param>
    /// <param name="unitOfWork">Coordenador transacional de persistência.</param>
    /// <param name="webhookOptions">Opções de configuração de segurança para validação de webhooks.</param>
    public ProcessPaymentWebhookCommandHandler(
        ITenantPaymentTransactionRepository transactionRepository,
        ITenantRepository tenantRepository,
        IUnitOfWork unitOfWork,
        IOptions<PaymentWebhookOptions> webhookOptions)
    {
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _webhookOptions = webhookOptions?.Value ?? throw new ArgumentNullException(nameof(webhookOptions));
    }

    /// <inheritdoc />
    public async Task<Result<PaymentWebhookResult>> Handle(ProcessPaymentWebhookCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // 1. Validação de token de autenticação quando configurado
        if (!string.IsNullOrWhiteSpace(_webhookOptions.WebhookToken) &&
            !string.Equals(_webhookOptions.WebhookToken, command.WebhookToken, StringComparison.Ordinal))
        {
            return Result<PaymentWebhookResult>.Failure(
                Error.Unauthorized("Webhook.Unauthorized", "Token de validação do webhook inválido."));
        }

        // 2. Localização da transação pelo ID externo do gateway
        var transaction = await _transactionRepository.GetByGatewayTransactionIdAsync(command.GatewayTransactionId, cancellationToken);
        if (transaction is null)
        {
            return Result<PaymentWebhookResult>.Failure(
                Error.NotFound("Webhook.TransactionNotFound", $"Transação com ID externo '{command.GatewayTransactionId}' não localizada."));
        }

        // 3. Garantia estrita de idempotência
        if (transaction.Status == PaymentTransactionStatus.Paid)
        {
            return Result<PaymentWebhookResult>.Success(new PaymentWebhookResult(
                IsProcessed: true,
                IsAlreadyProcessed: true,
                TransactionId: transaction.Id,
                Status: PaymentTransactionStatus.Paid));
        }

        // 4. Tratamento dos eventos de liquidação financeira
        var normalizedEvent = command.EventType.Trim().ToUpperInvariant();
        if (normalizedEvent is "PAYMENT_RECEIVED" or "PAYMENT_CONFIRMED")
        {
            var eventDate = command.EventDateUtc ?? DateTime.UtcNow;
            transaction.MarkAsPaid(eventDate, command.GatewayTransactionId);

            var tenant = await _tenantRepository.GetByIdAsync(transaction.TenantId, cancellationToken);
            if (tenant is not null)
            {
                var activateResult = tenant.ActivatePaidSubscription(
                    transaction.Tier,
                    transaction.BillingCycle,
                    eventDate,
                    transaction.Amount);

                if (activateResult.IsFailure)
                {
                    return Result<PaymentWebhookResult>.Failure(activateResult.Error);
                }
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            return Result<PaymentWebhookResult>.Success(new PaymentWebhookResult(
                IsProcessed: true,
                IsAlreadyProcessed: false,
                TransactionId: transaction.Id,
                Status: PaymentTransactionStatus.Paid));
        }

        if (normalizedEvent is "PAYMENT_OVERDUE" or "PAYMENT_FAILED")
        {
            transaction.MarkAsFailed($"Cobrança rejeitada ou vencida no gateway com evento '{command.EventType}'.");
            await _unitOfWork.CommitAsync(cancellationToken);

            return Result<PaymentWebhookResult>.Success(new PaymentWebhookResult(
                IsProcessed: true,
                IsAlreadyProcessed: false,
                TransactionId: transaction.Id,
                Status: PaymentTransactionStatus.Failed));
        }

        return Result<PaymentWebhookResult>.Success(new PaymentWebhookResult(
            IsProcessed: true,
            IsAlreadyProcessed: false,
            TransactionId: transaction.Id,
            Status: transaction.Status));
    }
}
