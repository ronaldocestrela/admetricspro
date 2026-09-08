using Master.Domain.Billing;
using Master.Domain.Tenants;

namespace WebApi.Models;

/// <summary>
/// Payload para requisição de processamento de checkout de assinatura comercial.
/// </summary>
/// <param name="TenantId">Identificador único do inquilino.</param>
/// <param name="Tier">Plano de assinatura pretendido (Starter, Pro ou Enterprise).</param>
/// <param name="BillingCycle">Ciclo de faturamento pretendido (Monthly ou Annual).</param>
/// <param name="PaymentMethod">Método de pagamento (CreditCard ou Pix).</param>
/// <param name="CardHolderName">Nome do titular conforme impresso no cartão (quando Cartão).</param>
/// <param name="CardNumber">Número do cartão de crédito (quando Cartão).</param>
/// <param name="ExpiryMonth">Mês de validade do cartão com 2 dígitos (quando Cartão).</param>
/// <param name="ExpiryYear">Ano de validade do cartão com 4 dígitos (quando Cartão).</param>
/// <param name="Ccv">Código de segurança CCV/CVV (quando Cartão).</param>
public sealed record ProcessCheckoutApiRequest(
    Guid TenantId,
    SubscriptionTier Tier,
    string BillingCycle,
    PaymentMethod PaymentMethod,
    string? CardHolderName = null,
    string? CardNumber = null,
    string? ExpiryMonth = null,
    string? ExpiryYear = null,
    string? Ccv = null);

/// <summary>
/// Resposta contendo os dados de pré-visualização de valores do checkout.
/// </summary>
/// <param name="Tier">Nível do plano pretendido.</param>
/// <param name="PlanName">Nome comercial do plano.</param>
/// <param name="BillingCycle">Ciclo de faturamento (Monthly ou Annual).</param>
/// <param name="BaseMonthlyPrice">Preço base mensal de tabela.</param>
/// <param name="DiscountPercentage">Percentual de desconto aplicado para ciclo anual.</param>
/// <param name="TotalPayableNow">Total líquido a ser liquidado imediatamente.</param>
/// <param name="SavingsAmount">Economia monetária calculada para o ciclo anual.</param>
/// <param name="NextRenewalDateUtc">Previsão da próxima renovação.</param>
public sealed record CheckoutPreviewApiResponse(
    SubscriptionTier Tier,
    string PlanName,
    string BillingCycle,
    decimal BaseMonthlyPrice,
    int DiscountPercentage,
    decimal TotalPayableNow,
    decimal SavingsAmount,
    DateTime NextRenewalDateUtc);

/// <summary>
/// Resposta contendo o resultado da tentativa de checkout.
/// </summary>
/// <param name="TransactionId">Identificador único da transação gerada.</param>
/// <param name="TenantId">Identificador do tenant contratante.</param>
/// <param name="Tier">Plano contratado.</param>
/// <param name="BillingCycle">Ciclo de faturamento.</param>
/// <param name="PaymentMethod">Método de pagamento utilizado.</param>
/// <param name="Amount">Valor total cobrado.</param>
/// <param name="Status">Estado atual de liquidação.</param>
/// <param name="IsActivated">Indica se a conta foi ativada imediatamente.</param>
/// <param name="PixQrCode">QR Code Pix em Base64 quando Pix.</param>
/// <param name="PixCopiaECola">Chave Copia e Cola Pix quando Pix.</param>
/// <param name="PixExpiresAtUtc">Data limite de expiração do Pix quando Pix.</param>
/// <param name="FailureReason">Mensagem explicativa caso o pagamento tenha falhado.</param>
public sealed record ProcessCheckoutApiResponse(
    Guid TransactionId,
    Guid TenantId,
    SubscriptionTier Tier,
    string BillingCycle,
    PaymentMethod PaymentMethod,
    decimal Amount,
    PaymentTransactionStatus Status,
    bool IsActivated,
    string? PixQrCode = null,
    string? PixCopiaECola = null,
    DateTime? PixExpiresAtUtc = null,
    string? FailureReason = null);

/// <summary>
/// Resposta contendo o status de liquidação consultado.
/// </summary>
/// <param name="TransactionId">Identificador da transação.</param>
/// <param name="Status">Estado atual da transação.</param>
/// <param name="IsPaid">Indica se a transação foi liquidada com sucesso.</param>
/// <param name="PaidAtUtc">Data e hora UTC da liquidação confirmada.</param>
public sealed record PaymentStatusApiResponse(
    Guid TransactionId,
    PaymentTransactionStatus Status,
    bool IsPaid,
    DateTime? PaidAtUtc);

/// <summary>
/// Payload recebido nos webhooks de gateways de pagamento (ex: Asaas).
/// </summary>
/// <param name="Event">Identificador do evento (ex: PAYMENT_RECEIVED, PAYMENT_CONFIRMED).</param>
/// <param name="Payment">Dados da cobrança associada ao evento.</param>
public sealed record AsaasWebhookApiPayload(
    string? Event,
    AsaasWebhookPaymentData? Payment);

/// <summary>
/// Dados da cobrança no payload de webhook do Asaas.
/// </summary>
/// <param name="Id">Identificador único da cobrança no gateway.</param>
/// <param name="Value">Valor monetário liquidado.</param>
/// <param name="PaymentDate">Data e hora do pagamento.</param>
public sealed record AsaasWebhookPaymentData(
    string? Id,
    decimal? Value,
    DateTime? PaymentDate);
