using BuildingBlocks.Domain.Primitives;
using Master.Application.Billing.Checkout.Commands.ProcessCheckout;
using Master.Application.Billing.Checkout.Queries.GetCheckoutPreview;
using Master.Application.Billing.Checkout.Queries.GetPaymentStatus;
using Master.Domain.Tenants;

namespace WebApp.Services;

/// <summary>
/// Contrato de serviço cliente para operações financeiras e checkout de assinatura no frontend.
/// Consome a Web API via cliente HTTP fortemente tipado com isolamento estrito de infraestrutura.
/// </summary>
public interface IBillingClientService
{
    /// <summary>
    /// Consulta os valores calculados, descontos aplicados e datas de renovação para pré-visualização de checkout.
    /// </summary>
    /// <param name="tenantId">Identificador único do inquilino.</param>
    /// <param name="tier">Nível de assinatura pretendido.</param>
    /// <param name="billingCycle">Ciclo de faturamento (Monthly ou Annual).</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Resultado contendo a discriminação de valores e descontos.</returns>
    Task<Result<CheckoutPreviewDto>> GetCheckoutPreviewAsync(
        Guid tenantId,
        SubscriptionTier tier,
        string billingCycle,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Envia a instrução de checkout com cartão de crédito ou emissão de Pix para ativação da assinatura.
    /// </summary>
    /// <param name="command">Dados da contratação e dados de pagamento.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Resultado da transação com status de liquidação ou dados do Pix gerado.</returns>
    Task<Result<ProcessCheckoutResult>> ProcessCheckoutAsync(
        ProcessCheckoutCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta o estado de liquidação de uma transação financeira de pagamento.
    /// </summary>
    /// <param name="transactionId">Identificador único da transação gerada.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Resultado com o status atualizado do pagamento.</returns>
    Task<Result<PaymentStatusDto>> GetPaymentStatusAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default);
}
