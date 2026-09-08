using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Billing.Payments;
using Master.Domain.Billing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Master.Infrastructure.Payments;

/// <summary>
/// Implementação de produção do <see cref="IPaymentGatewayService"/> integrando com a API v3 do gateway Asaas.
/// </summary>
public sealed class AsaasPaymentGateway : IPaymentGatewayService
{
    private readonly HttpClient _httpClient;
    private readonly AsaasOptions _options;
    private readonly ILogger<AsaasPaymentGateway> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Inicializa uma nova instância de <see cref="AsaasPaymentGateway"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP configurado com base URL e cabeçalho de acesso.</param>
    /// <param name="options">Opções de configuração do Asaas.</param>
    /// <param name="logger">Serviço de telemetria e log.</param>
    public AsaasPaymentGateway(
        HttpClient httpClient,
        IOptions<AsaasOptions> options,
        ILogger<AsaasPaymentGateway> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (!string.IsNullOrWhiteSpace(_options.ApiKey) && !_httpClient.DefaultRequestHeaders.Contains("access_token"))
        {
            _httpClient.DefaultRequestHeaders.Add("access_token", _options.ApiKey);
        }
    }

    /// <inheritdoc />
    public async Task<Result<PaymentGatewayCustomer>> GetOrCreateCustomerAsync(
        PaymentCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            // 1. Tentar localizar cliente por CPF/CNPJ
            var cleanCpfCnpj = new string(request.CpfCnpj.Where(char.IsDigit).ToArray());
            var searchResponse = await _httpClient.GetAsync($"customers?cpfCnpj={cleanCpfCnpj}", cancellationToken);

            if (searchResponse.IsSuccessStatusCode)
            {
                var searchBody = await searchResponse.Content.ReadFromJsonAsync<AsaasCustomerListResponse>(JsonOptions, cancellationToken);
                if (searchBody?.Data != null && searchBody.Data.Count > 0)
                {
                    return Result<PaymentGatewayCustomer>.Success(new PaymentGatewayCustomer(searchBody.Data[0].Id, "Asaas"));
                }
            }

            // 2. Criar novo cliente caso não exista
            var createPayload = new
            {
                name = request.Name,
                cpfCnpj = cleanCpfCnpj,
                email = request.Email,
                mobilePhone = request.Phone
            };

            var createResponse = await _httpClient.PostAsJsonAsync("customers", createPayload, JsonOptions, cancellationToken);
            if (!createResponse.IsSuccessStatusCode)
            {
                var errorBody = await createResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Falha ao registrar cliente no Asaas: {ErrorBody}", errorBody);
                return Result<PaymentGatewayCustomer>.Failure(
                    Error.Validation("PaymentGateway.CustomerCreationFailed", "Não foi possível registrar o cliente no gateway Asaas."));
            }

            var createdCustomer = await createResponse.Content.ReadFromJsonAsync<AsaasCustomerDto>(JsonOptions, cancellationToken);
            if (createdCustomer is null || string.IsNullOrWhiteSpace(createdCustomer.Id))
            {
                return Result<PaymentGatewayCustomer>.Failure(
                    Error.Failure("PaymentGateway.InvalidCustomerPayload", "Retorno inválido do gateway Asaas ao registrar cliente."));
            }

            return Result<PaymentGatewayCustomer>.Success(new PaymentGatewayCustomer(createdCustomer.Id, "Asaas"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro de comunicação ao registrar cliente no Asaas");
            return Result<PaymentGatewayCustomer>.Failure(
                Error.Failure("PaymentGateway.HttpError", "Falha de conexão com o gateway de pagamentos."));
        }
    }

    /// <inheritdoc />
    public async Task<Result<CreditCardChargeResult>> ChargeCreditCardAsync(
        CreditCardChargeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var payload = new
            {
                customer = request.CustomerId,
                billingType = "CREDIT_CARD",
                value = request.Amount,
                dueDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                description = request.Description,
                externalReference = request.ExternalReference,
                creditCard = new
                {
                    holderName = request.HolderName,
                    number = request.CardNumber,
                    expiryMonth = request.ExpiryMonth,
                    expiryYear = request.ExpiryYear,
                    ccv = request.Ccv
                }
            };

            var response = await _httpClient.PostAsJsonAsync("payments", payload, JsonOptions, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Pagamento de cartão recusado ou inválido no Asaas: {ResponseBody}", responseBody);
                return Result<CreditCardChargeResult>.Success(new CreditCardChargeResult(
                    TransactionId: string.Empty,
                    IsSuccess: false,
                    ErrorMessage: "A cobrança com cartão de crédito não foi autorizada pela operadora."));
            }

            var paymentDto = JsonSerializer.Deserialize<AsaasPaymentDto>(responseBody, JsonOptions);
            var isApproved = string.Equals(paymentDto?.Status, "CONFIRMED", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(paymentDto?.Status, "RECEIVED", StringComparison.OrdinalIgnoreCase);

            return Result<CreditCardChargeResult>.Success(new CreditCardChargeResult(
                TransactionId: paymentDto?.Id ?? string.Empty,
                IsSuccess: isApproved,
                AuthorizationCode: paymentDto?.Id,
                ErrorMessage: isApproved ? null : "Cobrança de cartão em análise pela adquirente."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao processar cartão no Asaas");
            return Result<CreditCardChargeResult>.Failure(
                Error.Failure("PaymentGateway.HttpError", "Falha de conexão com o gateway de pagamentos."));
        }
    }

    /// <inheritdoc />
    public async Task<Result<PixChargeResult>> CreatePixChargeAsync(
        PixChargeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            // 1. Criar cobrança com billingType = PIX
            var payload = new
            {
                customer = request.CustomerId,
                billingType = "PIX",
                value = request.Amount,
                dueDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                description = request.Description,
                externalReference = request.ExternalReference
            };

            var response = await _httpClient.PostAsJsonAsync("payments", payload, JsonOptions, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Falha ao criar cobrança Pix no Asaas: {ErrorBody}", errorBody);
                return Result<PixChargeResult>.Failure(
                    Error.Validation("PaymentGateway.PixCreationFailed", "Não foi possível gerar a cobrança Pix no gateway."));
            }

            var paymentDto = await response.Content.ReadFromJsonAsync<AsaasPaymentDto>(JsonOptions, cancellationToken);
            if (paymentDto is null || string.IsNullOrWhiteSpace(paymentDto.Id))
            {
                return Result<PixChargeResult>.Failure(
                    Error.Failure("PaymentGateway.InvalidPixPayload", "Retorno inválido ao gerar cobrança Pix."));
            }

            // 2. Obter QR Code e chave Copia e Cola Pix
            var qrCodeResponse = await _httpClient.GetAsync($"payments/{paymentDto.Id}/pixQrCode", cancellationToken);
            if (!qrCodeResponse.IsSuccessStatusCode)
            {
                return Result<PixChargeResult>.Failure(
                    Error.Failure("PaymentGateway.PixQrCodeError", "Não foi possível resgatar o QR Code Pix gerado."));
            }

            var qrCodeDto = await qrCodeResponse.Content.ReadFromJsonAsync<AsaasPixQrCodeDto>(JsonOptions, cancellationToken);
            if (qrCodeDto is null || string.IsNullOrWhiteSpace(qrCodeDto.Payload))
            {
                return Result<PixChargeResult>.Failure(
                    Error.Failure("PaymentGateway.InvalidQrCodePayload", "Payload Copia e Cola não retornado pelo gateway."));
            }

            var expiresAtUtc = qrCodeDto.ExpirationDate ?? DateTime.UtcNow.AddHours(24);

            return Result<PixChargeResult>.Success(new PixChargeResult(
                TransactionId: paymentDto.Id,
                QrCodeBase64: qrCodeDto.EncodedImage ?? string.Empty,
                CopiaECola: qrCodeDto.Payload,
                ExpiresAtUtc: expiresAtUtc));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro de comunicação ao emitir Pix no Asaas");
            return Result<PixChargeResult>.Failure(
                Error.Failure("PaymentGateway.HttpError", "Falha de conexão com o gateway de pagamentos."));
        }
    }

    /// <inheritdoc />
    public async Task<Result<PaymentStatusResult>> GetPaymentStatusAsync(
        string gatewayTransactionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(gatewayTransactionId))
        {
            return Result<PaymentStatusResult>.Failure(
                Error.Validation("PaymentGateway.InvalidTransactionId", "O identificador da transação é obrigatório."));
        }

        try
        {
            var response = await _httpClient.GetAsync($"payments/{gatewayTransactionId}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return Result<PaymentStatusResult>.Failure(
                    Error.NotFound("PaymentGateway.TransactionNotFound", "Transação não localizada no gateway."));
            }

            var paymentDto = await response.Content.ReadFromJsonAsync<AsaasPaymentDto>(JsonOptions, cancellationToken);
            if (paymentDto is null)
            {
                return Result<PaymentStatusResult>.Failure(
                    Error.Failure("PaymentGateway.InvalidPayload", "Retorno inválido do gateway."));
            }

            var status = paymentDto.Status switch
            {
                "RECEIVED" or "CONFIRMED" => PaymentTransactionStatus.Paid,
                "OVERDUE" => PaymentTransactionStatus.Failed,
                "REFUNDED" => PaymentTransactionStatus.Refunded,
                _ => PaymentTransactionStatus.Pending
            };

            return Result<PaymentStatusResult>.Success(new PaymentStatusResult(
                TransactionId: gatewayTransactionId,
                Status: status,
                PaidAtUtc: status == PaymentTransactionStatus.Paid ? (paymentDto.PaymentDate ?? DateTime.UtcNow) : null));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao consultar status da transação {TransactionId} no Asaas", gatewayTransactionId);
            return Result<PaymentStatusResult>.Failure(
                Error.Failure("PaymentGateway.HttpError", "Falha de conexão com o gateway de pagamentos."));
        }
    }

    private sealed record AsaasCustomerListResponse(
        [property: JsonPropertyName("data")] List<AsaasCustomerDto> Data);

    private sealed record AsaasCustomerDto(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("name")] string Name);

    private sealed record AsaasPaymentDto(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("paymentDate")] DateTime? PaymentDate);

    private sealed record AsaasPixQrCodeDto(
        [property: JsonPropertyName("encodedImage")] string? EncodedImage,
        [property: JsonPropertyName("payload")] string? Payload,
        [property: JsonPropertyName("expirationDate")] DateTime? ExpirationDate);
}
