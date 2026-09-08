using BuildingBlocks.Application.Messaging;

namespace Master.Application.Billing.Checkout.Commands.ProcessWebhook;

/// <summary>
/// Comando para processar eventos recebidos via webhook dos gateways de pagamento (ex: Asaas, Stripe).
/// </summary>
/// <param name="Provider">Nome do provedor emissor da notificação (ex.: Asaas).</param>
/// <param name="WebhookToken">Token de segurança enviado no cabeçalho HTTP.</param>
/// <param name="EventType">Tipo do evento (ex.: PAYMENT_RECEIVED, PAYMENT_CONFIRMED).</param>
/// <param name="GatewayTransactionId">Identificador único da transação no gateway.</param>
/// <param name="Amount">Valor monetário reportado na notificação se fornecido.</param>
/// <param name="EventDateUtc">Data e hora do evento em UTC se fornecido.</param>
public sealed record ProcessPaymentWebhookCommand(
    string Provider,
    string? WebhookToken,
    string EventType,
    string GatewayTransactionId,
    decimal? Amount = null,
    DateTime? EventDateUtc = null) : ICommand<PaymentWebhookResult>;
