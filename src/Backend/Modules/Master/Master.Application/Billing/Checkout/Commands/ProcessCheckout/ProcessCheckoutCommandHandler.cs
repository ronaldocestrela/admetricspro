using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Messaging.Persistence;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Billing.Payments;
using Master.Application.Messaging;
using Master.Application.Repositories;
using Master.Domain.Billing;
using Master.Domain.Tenants;

namespace Master.Application.Billing.Checkout.Commands.ProcessCheckout;

/// <summary>
/// Manipulador responsável pela orquestração do checkout de pagamento, integração com gateway e ativação do inquilino.
/// </summary>
public sealed class ProcessCheckoutCommandHandler : ICommandHandler<ProcessCheckoutCommand, ProcessCheckoutResult>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IPlanRepository _planRepository;
    private readonly ITenantPaymentTransactionRepository _transactionRepository;
    private readonly IPaymentGatewayService _paymentGateway;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICatalogIntegrationOutboxWriter? _catalogOutboxWriter;
    private readonly IMessagingFeatureGate? _messagingFeatureGate;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ProcessCheckoutCommandHandler"/>.
    /// </summary>
    /// <param name="tenantRepository">Repositório de inquilinos.</param>
    /// <param name="planRepository">Repositório de planos de assinatura.</param>
    /// <param name="transactionRepository">Repositório de transações financeiras.</param>
    /// <param name="paymentGateway">Serviço de integração com gateway de pagamento.</param>
    /// <param name="unitOfWork">Coordenador transacional de persistência.</param>
    /// <param name="catalogOutboxWriter">Gravador de outbox no MasterDb.</param>
    /// <param name="messagingFeatureGate">Feature flag de mensageria.</param>
    public ProcessCheckoutCommandHandler(
        ITenantRepository tenantRepository,
        IPlanRepository planRepository,
        ITenantPaymentTransactionRepository transactionRepository,
        IPaymentGatewayService paymentGateway,
        IUnitOfWork unitOfWork,
        ICatalogIntegrationOutboxWriter? catalogOutboxWriter = null,
        IMessagingFeatureGate? messagingFeatureGate = null)
    {
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _planRepository = planRepository ?? throw new ArgumentNullException(nameof(planRepository));
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _paymentGateway = paymentGateway ?? throw new ArgumentNullException(nameof(paymentGateway));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _catalogOutboxWriter = catalogOutboxWriter;
        _messagingFeatureGate = messagingFeatureGate;
    }

    /// <inheritdoc />
    public async Task<Result<ProcessCheckoutResult>> Handle(ProcessCheckoutCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var tenant = await _tenantRepository.GetByIdAsync(new TenantId(command.TenantId), cancellationToken);
        if (tenant is null)
        {
            return Result<ProcessCheckoutResult>.Failure(
                Error.NotFound("Tenant.NotFound", "Inquilino não localizado no catálogo Master."));
        }

        var isAnnual = string.Equals(command.BillingCycle?.Trim(), "Annual", StringComparison.OrdinalIgnoreCase);
        var normalizedCycle = isAnnual ? "Annual" : "Monthly";

        var plan = await _planRepository.GetByTierAsync(command.Tier, cancellationToken);
        var monthlyPrice = plan?.MonthlyPrice ?? GetDefaultMonthlyPrice(command.Tier);
        var discountPct = isAnnual ? (plan?.AnnualDiscountPercentage ?? 20) : 0;

        decimal amount;
        if (isAnnual)
        {
            var fullYearAmount = monthlyPrice * 12m;
            var discountFactor = (100m - discountPct) / 100m;
            amount = Math.Round(fullYearAmount * discountFactor, 2);
        }
        else
        {
            amount = monthlyPrice;
        }

        // 1. Obter ou registrar cliente no gateway de pagamentos
        var customerRequest = new PaymentCustomerRequest(
            tenant.CompanyName,
            tenant.Cnpj,
            tenant.AdminEmail ?? $"contato@{tenant.Subdomain}.admetricspro.internal");

        var customerResult = await _paymentGateway.GetOrCreateCustomerAsync(customerRequest, cancellationToken);
        if (customerResult.IsFailure)
        {
            return Result<ProcessCheckoutResult>.Failure(customerResult.Error);
        }

        var customer = customerResult.Value;

        // 2. Processar conforme método de pagamento
        if (command.PaymentMethod == PaymentMethod.CreditCard)
        {
            var cardRequest = new CreditCardChargeRequest(
                customer.CustomerId,
                amount,
                $"AdMetricsPro - Plano {command.Tier} ({normalizedCycle})",
                tenant.Id.Value.ToString(),
                command.CardHolderName ?? tenant.CompanyName,
                command.CardNumber ?? string.Empty,
                command.ExpiryMonth ?? string.Empty,
                command.ExpiryYear ?? string.Empty,
                command.Ccv ?? string.Empty);

            var chargeResult = await _paymentGateway.ChargeCreditCardAsync(cardRequest, cancellationToken);
            if (chargeResult.IsFailure)
            {
                return Result<ProcessCheckoutResult>.Failure(chargeResult.Error);
            }

            var cardCharge = chargeResult.Value;
            var txResult = TenantPaymentTransaction.CreateCreditCard(
                tenant.Id,
                command.Tier,
                normalizedCycle,
                amount,
                customer.Provider,
                cardCharge.TransactionId);

            if (txResult.IsFailure)
            {
                return Result<ProcessCheckoutResult>.Failure(txResult.Error);
            }

            var transaction = txResult.Value;

            if (cardCharge.IsSuccess)
            {
                transaction.MarkAsPaid(DateTime.UtcNow, cardCharge.TransactionId);
                var activateResult = tenant.ActivatePaidSubscription(command.Tier, normalizedCycle, DateTime.UtcNow, amount);
                if (activateResult.IsFailure)
                {
                    return Result<ProcessCheckoutResult>.Failure(activateResult.Error);
                }

                await _transactionRepository.AddAsync(transaction, cancellationToken);

                var enqueueResult = await TryEnqueueSubscriptionConfirmationAsync(tenant, cancellationToken);
                if (enqueueResult.IsFailure)
                {
                    return Result<ProcessCheckoutResult>.Failure(enqueueResult.Error);
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<ProcessCheckoutResult>.Success(new ProcessCheckoutResult(
                    transaction.Id,
                    tenant.Id.Value,
                    command.Tier,
                    normalizedCycle,
                    PaymentMethod.CreditCard,
                    amount,
                    PaymentTransactionStatus.Paid,
                    IsActivated: true));
            }

            transaction.MarkAsFailed(cardCharge.ErrorMessage ?? "Cartão recusado pela operadora.");
            await _transactionRepository.AddAsync(transaction, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            return Result<ProcessCheckoutResult>.Success(new ProcessCheckoutResult(
                transaction.Id,
                tenant.Id.Value,
                command.Tier,
                normalizedCycle,
                PaymentMethod.CreditCard,
                amount,
                PaymentTransactionStatus.Failed,
                IsActivated: false,
                FailureReason: cardCharge.ErrorMessage ?? "Cartão recusado pela operadora."));
        }

        // Método Pix
        var pixRequest = new PixChargeRequest(
            customer.CustomerId,
            amount,
            $"AdMetricsPro - Plano {command.Tier} ({normalizedCycle})",
            tenant.Id.Value.ToString());

        var pixChargeResult = await _paymentGateway.CreatePixChargeAsync(pixRequest, cancellationToken);
        if (pixChargeResult.IsFailure)
        {
            return Result<ProcessCheckoutResult>.Failure(pixChargeResult.Error);
        }

        var pixCharge = pixChargeResult.Value;
        var pixTxResult = TenantPaymentTransaction.CreatePix(
            tenant.Id,
            command.Tier,
            normalizedCycle,
            amount,
            customer.Provider,
            pixCharge.QrCodeBase64,
            pixCharge.CopiaECola,
            pixCharge.ExpiresAtUtc,
            pixCharge.TransactionId);

        if (pixTxResult.IsFailure)
        {
            return Result<ProcessCheckoutResult>.Failure(pixTxResult.Error);
        }

        var pixTransaction = pixTxResult.Value;
        await _transactionRepository.AddAsync(pixTransaction, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result<ProcessCheckoutResult>.Success(new ProcessCheckoutResult(
            pixTransaction.Id,
            tenant.Id.Value,
            command.Tier,
            normalizedCycle,
            PaymentMethod.Pix,
            amount,
            PaymentTransactionStatus.Pending,
            IsActivated: false,
            PixQrCode: pixCharge.QrCodeBase64,
            PixCopiaECola: pixCharge.CopiaECola,
            PixExpiresAtUtc: pixCharge.ExpiresAtUtc));
    }

    private async Task<Result> TryEnqueueSubscriptionConfirmationAsync(Tenant tenant, CancellationToken cancellationToken)
    {
        if (_messagingFeatureGate?.IsEnabled != true || string.IsNullOrWhiteSpace(tenant.AdminEmail))
        {
            return Result.Success();
        }

        if (_catalogOutboxWriter is null)
        {
            return Result.Failure(
                Error.Failure("Messaging.OutboxUnavailable", "Gravador de outbox do catálogo não configurado."));
        }

        var outboxResult = CatalogIntegrationOutboxFactory.CreateSubscriptionConfirmationOutbox(
            tenant.Id.Value,
            tenant.Id.Value,
            DateTime.UtcNow);
        if (outboxResult.IsFailure)
        {
            return Result.Failure(outboxResult.Error);
        }

        return await _catalogOutboxWriter.AddAsync(outboxResult.Value, cancellationToken);
    }

    private static decimal GetDefaultMonthlyPrice(SubscriptionTier tier) => tier switch
    {
        SubscriptionTier.Starter => 197.00m,
        SubscriptionTier.Pro => 497.00m,
        SubscriptionTier.Enterprise => 1497.00m,
        _ => 0m
    };
}
