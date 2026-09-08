using Master.Domain.Billing;

namespace Master.Application.Billing.Payments;

/// <summary>
/// Dados para registro ou consulta de cliente no gateway de pagamentos.
/// </summary>
/// <param name="Name">Nome ou razão social do cliente.</param>
/// <param name="CpfCnpj">CPF ou CNPJ (somente dígitos).</param>
/// <param name="Email">E-mail corporativo para envio de comprovantes.</param>
/// <param name="Phone">Telefone de contato com DDD opcional.</param>
public sealed record PaymentCustomerRequest(
    string Name,
    string CpfCnpj,
    string Email,
    string? Phone = null);

/// <summary>
/// Identificador e dados do cliente registrado no gateway.
/// </summary>
/// <param name="CustomerId">Identificador único atribuído pelo provedor.</param>
/// <param name="Provider">Nome do provedor de pagamento (ex.: Asaas, InMemory).</param>
public sealed record PaymentGatewayCustomer(
    string CustomerId,
    string Provider);

/// <summary>
/// Parâmetros para processamento de cobrança via Cartão de Crédito.
/// </summary>
/// <param name="CustomerId">Identificador do cliente no gateway.</param>
/// <param name="Amount">Valor monetário total da cobrança.</param>
/// <param name="Description">Descrição exibida no extrato/fatura.</param>
/// <param name="ExternalReference">Referência externa (ex.: ID da transação no MasterDb).</param>
/// <param name="HolderName">Nome do titular impresso no cartão.</param>
/// <param name="CardNumber">Número do cartão de crédito.</param>
/// <param name="ExpiryMonth">Mês de vencimento (2 dígitos: 01 a 12).</param>
/// <param name="ExpiryYear">Ano de vencimento (4 dígitos: ex. 2030).</param>
/// <param name="Ccv">Código de verificação (CVV/CVC).</param>
public sealed record CreditCardChargeRequest(
    string CustomerId,
    decimal Amount,
    string Description,
    string ExternalReference,
    string HolderName,
    string CardNumber,
    string ExpiryMonth,
    string ExpiryYear,
    string Ccv);

/// <summary>
/// Resultado da tentativa de cobrança via Cartão de Crédito.
/// </summary>
/// <param name="TransactionId">Identificador da transação no provedor.</param>
/// <param name="IsSuccess">Indica se a transação foi aprovada e capturada com sucesso.</param>
/// <param name="AuthorizationCode">Código de autorização da adquirente em caso de aprovação.</param>
/// <param name="ErrorMessage">Mensagem de recusa ou erro detalhado retornado pelo gateway.</param>
public sealed record CreditCardChargeResult(
    string TransactionId,
    bool IsSuccess,
    string? AuthorizationCode = null,
    string? ErrorMessage = null);

/// <summary>
/// Parâmetros para geração de cobrança instantânea via Pix.
/// </summary>
/// <param name="CustomerId">Identificador do cliente no gateway.</param>
/// <param name="Amount">Valor monetário total da cobrança.</param>
/// <param name="Description">Descrição exibida no aplicativo bancário.</param>
/// <param name="ExternalReference">Referência externa (ID da transação no MasterDb).</param>
public sealed record PixChargeRequest(
    string CustomerId,
    decimal Amount,
    string Description,
    string ExternalReference);

/// <summary>
/// Resultado da emissão de cobrança via Pix com elementos de liquidação.
/// </summary>
/// <param name="TransactionId">Identificador da cobrança no provedor.</param>
/// <param name="QrCodeBase64">Imagem do QR Code Pix codificada em Base64.</param>
/// <param name="CopiaECola">Código alfanumérico Copia e Cola (payload EMV).</param>
/// <param name="ExpiresAtUtc">Data e hora limite de expiração do Pix em UTC.</param>
public sealed record PixChargeResult(
    string TransactionId,
    string QrCodeBase64,
    string CopiaECola,
    DateTime ExpiresAtUtc);

/// <summary>
/// Resultado da consulta de status de liquidação de uma transação.
/// </summary>
/// <param name="TransactionId">Identificador da cobrança no provedor.</param>
/// <param name="Status">Estado atual de liquidação.</param>
/// <param name="PaidAtUtc">Data e hora da confirmação do pagamento se liquidado.</param>
public sealed record PaymentStatusResult(
    string TransactionId,
    PaymentTransactionStatus Status,
    DateTime? PaidAtUtc);
