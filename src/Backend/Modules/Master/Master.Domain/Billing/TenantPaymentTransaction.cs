using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;
using Master.Domain.Tenants;

namespace Master.Domain.Billing;

/// <summary>
/// Representa uma transação financeira de pagamento e assinatura de um inquilino no catálogo Master.
/// </summary>
public sealed class TenantPaymentTransaction : Entity<Guid>
{
    private TenantPaymentTransaction(
        Guid id,
        TenantId tenantId,
        SubscriptionTier tier,
        string billingCycle,
        decimal amount,
        PaymentMethod paymentMethod,
        PaymentTransactionStatus status,
        string gatewayProvider,
        string? gatewayTransactionId,
        string? pixQrCode,
        string? pixCopiaECola,
        DateTime? pixExpiresAtUtc,
        DateTime createdAtUtc)
        : base(id)
    {
        TenantId = tenantId;
        Tier = tier;
        BillingCycle = billingCycle;
        Amount = amount;
        PaymentMethod = paymentMethod;
        Status = status;
        GatewayProvider = gatewayProvider;
        GatewayTransactionId = gatewayTransactionId;
        PixQrCode = pixQrCode;
        PixCopiaECola = pixCopiaECola;
        PixExpiresAtUtc = pixExpiresAtUtc;
        CreatedAtUtc = createdAtUtc;
    }

    private TenantPaymentTransaction()
        : base(Guid.NewGuid())
    {
        TenantId = new TenantId(Guid.Empty);
        Tier = SubscriptionTier.Starter;
        BillingCycle = "Monthly";
        Amount = 0m;
        PaymentMethod = PaymentMethod.CreditCard;
        Status = PaymentTransactionStatus.Pending;
        GatewayProvider = string.Empty;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Identificador do tenant titular da transação.
    /// </summary>
    public TenantId TenantId { get; private set; }

    /// <summary>
    /// Nível de assinatura contratado.
    /// </summary>
    public SubscriptionTier Tier { get; private set; }

    /// <summary>
    /// Ciclo de faturamento contratado (Monthly ou Annual).
    /// </summary>
    public string BillingCycle { get; private set; }

    /// <summary>
    /// Valor monetário nominal cobrado em Reais (BRL).
    /// </summary>
    public decimal Amount { get; private set; }

    /// <summary>
    /// Método de pagamento empregado (Cartão de Crédito ou Pix).
    /// </summary>
    public PaymentMethod PaymentMethod { get; private set; }

    /// <summary>
    /// Estado atual de processamento da transação financeira.
    /// </summary>
    public PaymentTransactionStatus Status { get; private set; }

    /// <summary>
    /// Nome identificador do provedor de gateway (ex.: "Asaas", "InMemory").
    /// </summary>
    public string GatewayProvider { get; private set; }

    /// <summary>
    /// Identificador externo da cobrança atribuído pelo gateway de pagamentos.
    /// </summary>
    public string? GatewayTransactionId { get; private set; }

    /// <summary>
    /// Imagem do QR Code Pix codificada em Base64, quando aplicável.
    /// </summary>
    public string? PixQrCode { get; private set; }

    /// <summary>
    /// Código alfanumérico Copia e Cola padrão EMV do Pix, quando aplicável.
    /// </summary>
    public string? PixCopiaECola { get; private set; }

    /// <summary>
    /// Data e hora limite UTC para liquidação do QR Code Pix.
    /// </summary>
    public DateTime? PixExpiresAtUtc { get; private set; }

    /// <summary>
    /// Data e hora UTC de criação do registro da cobrança.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Data e hora UTC da liquidação financeira confirmada.
    /// </summary>
    public DateTime? PaidAtUtc { get; private set; }

    /// <summary>
    /// Justificativa detalhada em caso de falha ou recusa da cobrança.
    /// </summary>
    public string? FailureReason { get; private set; }

    /// <summary>
    /// Cria uma nova transação financeira configurada para cobrança via Cartão de Crédito.
    /// </summary>
    /// <param name="tenantId">Identificador do tenant contratante.</param>
    /// <param name="tier">Nível do plano contratado.</param>
    /// <param name="billingCycle">Ciclo de faturamento (Monthly ou Annual).</param>
    /// <param name="amount">Valor a ser cobrado.</param>
    /// <param name="gatewayProvider">Provedor do gateway de pagamento.</param>
    /// <param name="gatewayTransactionId">Identificador prévio no gateway se houver.</param>
    /// <returns>Resultado contendo a transação criada ou falha de validação.</returns>
    public static Result<TenantPaymentTransaction> CreateCreditCard(
        TenantId tenantId,
        SubscriptionTier tier,
        string billingCycle,
        decimal amount,
        string gatewayProvider,
        string? gatewayTransactionId = null)
    {
        var validationResult = ValidateCommon(tier, billingCycle, amount, gatewayProvider);
        if (validationResult.IsFailure)
        {
            return Result<TenantPaymentTransaction>.Failure(validationResult.Error);
        }

        var transaction = new TenantPaymentTransaction(
            Guid.NewGuid(),
            tenantId,
            tier,
            validationResult.Value,
            amount,
            PaymentMethod.CreditCard,
            PaymentTransactionStatus.Pending,
            gatewayProvider.Trim(),
            gatewayTransactionId?.Trim(),
            pixQrCode: null,
            pixCopiaECola: null,
            pixExpiresAtUtc: null,
            DateTime.UtcNow);

        return Result<TenantPaymentTransaction>.Success(transaction);
    }

    /// <summary>
    /// Cria uma nova transação financeira configurada para cobrança instantânea via Pix.
    /// </summary>
    /// <param name="tenantId">Identificador do tenant contratante.</param>
    /// <param name="tier">Nível do plano contratado.</param>
    /// <param name="billingCycle">Ciclo de faturamento (Monthly ou Annual).</param>
    /// <param name="amount">Valor a ser cobrado.</param>
    /// <param name="gatewayProvider">Provedor do gateway de pagamento.</param>
    /// <param name="pixQrCode">QR Code Pix em Base64.</param>
    /// <param name="pixCopiaECola">Chave Copia e Cola Pix.</param>
    /// <param name="pixExpiresAtUtc">Data e hora de expiração do Pix.</param>
    /// <param name="gatewayTransactionId">Identificador prévio no gateway se houver.</param>
    /// <returns>Resultado contendo a transação criada ou falha de validação.</returns>
    public static Result<TenantPaymentTransaction> CreatePix(
        TenantId tenantId,
        SubscriptionTier tier,
        string billingCycle,
        decimal amount,
        string gatewayProvider,
        string? pixQrCode,
        string? pixCopiaECola,
        DateTime? pixExpiresAtUtc,
        string? gatewayTransactionId = null)
    {
        var validationResult = ValidateCommon(tier, billingCycle, amount, gatewayProvider);
        if (validationResult.IsFailure)
        {
            return Result<TenantPaymentTransaction>.Failure(validationResult.Error);
        }

        var transaction = new TenantPaymentTransaction(
            Guid.NewGuid(),
            tenantId,
            tier,
            validationResult.Value,
            amount,
            PaymentMethod.Pix,
            PaymentTransactionStatus.Pending,
            gatewayProvider.Trim(),
            gatewayTransactionId?.Trim(),
            pixQrCode?.Trim(),
            pixCopiaECola?.Trim(),
            pixExpiresAtUtc,
            DateTime.UtcNow);

        return Result<TenantPaymentTransaction>.Success(transaction);
    }

    /// <summary>
    /// Marca a transação financeira como paga e liquidada com sucesso.
    /// </summary>
    /// <param name="paidAtUtc">Data e hora UTC da liquidação.</param>
    /// <param name="gatewayTransactionId">Identificador único atribuído pelo gateway.</param>
    /// <returns>Resultado de sucesso ou erro caso a transação já tenha sido processada.</returns>
    public Result MarkAsPaid(DateTime paidAtUtc, string gatewayTransactionId)
    {
        if (Status == PaymentTransactionStatus.Paid)
        {
            return Result.Failure(Error.Conflict("PaymentTransaction.AlreadyProcessed", "A transação já se encontra liquidada."));
        }

        Status = PaymentTransactionStatus.Paid;
        PaidAtUtc = paidAtUtc;

        if (!string.IsNullOrWhiteSpace(gatewayTransactionId))
        {
            GatewayTransactionId = gatewayTransactionId.Trim();
        }

        return Result.Success();
    }

    /// <summary>
    /// Marca a transação financeira como falha com justificativa.
    /// </summary>
    /// <param name="failureReason">Motivo da recusa ou falha reportado pelo gateway.</param>
    /// <returns>Resultado de sucesso ou erro.</returns>
    public Result MarkAsFailed(string failureReason)
    {
        if (Status == PaymentTransactionStatus.Paid)
        {
            return Result.Failure(Error.Conflict("PaymentTransaction.AlreadyProcessed", "Não é possível marcar como falha uma transação já liquidada."));
        }

        Status = PaymentTransactionStatus.Failed;
        FailureReason = string.IsNullOrWhiteSpace(failureReason) ? "Cobrança não aprovada pelo gateway." : failureReason.Trim();
        return Result.Success();
    }

    private static Result<string> ValidateCommon(
        SubscriptionTier tier,
        string billingCycle,
        decimal amount,
        string gatewayProvider)
    {
        if (amount <= 0)
        {
            return Result<string>.Failure(Error.Validation("PaymentTransaction.InvalidAmount", "O valor da transação deve ser positivo."));
        }

        if (tier == SubscriptionTier.Trial)
        {
            return Result<string>.Failure(Error.Validation("PaymentTransaction.InvalidTier", "Não é permitido gerar transação financeira para o plano Trial."));
        }

        if (string.IsNullOrWhiteSpace(billingCycle))
        {
            return Result<string>.Failure(Error.Validation("PaymentTransaction.InvalidBillingCycle", "O ciclo de faturamento é obrigatório."));
        }

        var normalizedCycle = billingCycle.Trim();
        if (!string.Equals(normalizedCycle, "Monthly", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(normalizedCycle, "Annual", StringComparison.OrdinalIgnoreCase))
        {
            return Result<string>.Failure(Error.Validation("PaymentTransaction.InvalidBillingCycle", "O ciclo de faturamento deve ser Monthly ou Annual."));
        }

        if (string.IsNullOrWhiteSpace(gatewayProvider))
        {
            return Result<string>.Failure(Error.Validation("PaymentTransaction.ProviderRequired", "O provedor do gateway é obrigatório."));
        }

        return Result<string>.Success(string.Equals(normalizedCycle, "Monthly", StringComparison.OrdinalIgnoreCase) ? "Monthly" : "Annual");
    }
}
