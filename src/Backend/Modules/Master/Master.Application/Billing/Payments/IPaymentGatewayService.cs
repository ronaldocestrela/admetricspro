using BuildingBlocks.Domain.Primitives;

namespace Master.Application.Billing.Payments;

/// <summary>
/// Contrato de integração com provedores de gateway de pagamento para emissão e liquidação de assinaturas comerciais.
/// </summary>
public interface IPaymentGatewayService
{
    /// <summary>
    /// Obtém ou registra um cliente no gateway de pagamentos.
    /// </summary>
    /// <param name="request">Dados cadastrais do cliente.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    /// <returns>Dados do cliente registrado no provedor.</returns>
    Task<Result<PaymentGatewayCustomer>> GetOrCreateCustomerAsync(PaymentCustomerRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Realiza a autorização e captura de cobrança via Cartão de Crédito.
    /// </summary>
    /// <param name="request">Parâmetros da cobrança e dados do cartão.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    /// <returns>Resultado da transação com código de autorização ou erro explicativo.</returns>
    Task<Result<CreditCardChargeResult>> ChargeCreditCardAsync(CreditCardChargeRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Emite uma cobrança instantânea via Pix, gerando QR Code e payload Copia e Cola.
    /// </summary>
    /// <param name="request">Parâmetros da cobrança.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    /// <returns>Resultado contendo QR Code em Base64 e chave Copia e Cola.</returns>
    Task<Result<PixChargeResult>> CreatePixChargeAsync(PixChargeRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta o estado atual de liquidação de uma transação no provedor.
    /// </summary>
    /// <param name="gatewayTransactionId">Identificador único atribuído pelo provedor.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    /// <returns>Estado da transação e data de liquidação quando aplicável.</returns>
    Task<Result<PaymentStatusResult>> GetPaymentStatusAsync(string gatewayTransactionId, CancellationToken cancellationToken = default);
}
