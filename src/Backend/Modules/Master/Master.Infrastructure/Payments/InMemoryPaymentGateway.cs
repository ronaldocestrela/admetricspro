using System.Collections.Concurrent;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Billing.Payments;
using Master.Domain.Billing;

namespace Master.Infrastructure.Payments;

/// <summary>
/// Implementação em memória de <see cref="IPaymentGatewayService"/> para testes automatizados, CI/CD e desenvolvimento local.
/// </summary>
public sealed class InMemoryPaymentGateway : IPaymentGatewayService
{
    private readonly ConcurrentDictionary<string, PaymentStatusResult> _transactions = new();
    private readonly ConcurrentDictionary<string, string> _customers = new();

    /// <inheritdoc />
    public Task<Result<PaymentGatewayCustomer>> GetOrCreateCustomerAsync(
        PaymentCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var customerId = _customers.GetOrAdd(request.CpfCnpj, _ => $"cus_mock_{Guid.NewGuid():N}"[..16]);
        return Task.FromResult(Result<PaymentGatewayCustomer>.Success(new PaymentGatewayCustomer(customerId, "InMemory")));
    }

    /// <inheritdoc />
    public Task<Result<CreditCardChargeResult>> ChargeCreditCardAsync(
        CreditCardChargeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var txId = $"tx_mock_{Guid.NewGuid():N}"[..20];

        if (request.CardNumber.EndsWith("0002") || request.CardNumber.EndsWith("9999"))
        {
            var failedResult = new CreditCardChargeResult(
                txId,
                IsSuccess: false,
                ErrorMessage: "Transação de cartão recusada pelo emissor (saldo insuficiente ou suspeita de fraude).");

            _transactions[txId] = new PaymentStatusResult(txId, PaymentTransactionStatus.Failed, null);
            return Task.FromResult(Result<CreditCardChargeResult>.Success(failedResult));
        }

        var successResult = new CreditCardChargeResult(
            txId,
            IsSuccess: true,
            AuthorizationCode: $"AUTH_{Guid.NewGuid():N}"[..10].ToUpperInvariant());

        _transactions[txId] = new PaymentStatusResult(txId, PaymentTransactionStatus.Paid, DateTime.UtcNow);
        return Task.FromResult(Result<CreditCardChargeResult>.Success(successResult));
    }

    /// <inheritdoc />
    public Task<Result<PixChargeResult>> CreatePixChargeAsync(
        PixChargeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var txId = $"pix_mock_{Guid.NewGuid():N}"[..20];
        var qrCodeBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";
        var copiaECola = $"00020126580014br.gov.bcb.pix0136{Guid.NewGuid()}520400005303986540{request.Amount:F2}5802BR5913AdMetricsPro6009SaoPaulo62070503***6304";
        var expiresAtUtc = DateTime.UtcNow.AddHours(24);

        _transactions[txId] = new PaymentStatusResult(txId, PaymentTransactionStatus.Pending, null);

        var result = new PixChargeResult(txId, qrCodeBase64, copiaECola, expiresAtUtc);
        return Task.FromResult(Result<PixChargeResult>.Success(result));
    }

    /// <inheritdoc />
    public Task<Result<PaymentStatusResult>> GetPaymentStatusAsync(
        string gatewayTransactionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(gatewayTransactionId))
        {
            return Task.FromResult(Result<PaymentStatusResult>.Failure(
                Error.Validation("PaymentGateway.InvalidTransactionId", "O identificador da transação é obrigatório.")));
        }

        if (_transactions.TryGetValue(gatewayTransactionId, out var status))
        {
            return Task.FromResult(Result<PaymentStatusResult>.Success(status));
        }

        return Task.FromResult(Result<PaymentStatusResult>.Success(
            new PaymentStatusResult(gatewayTransactionId, PaymentTransactionStatus.Pending, null)));
    }

    /// <summary>
    /// Simula a liquidação financeira de uma transação pendente para testes de integração e cenários assíncronos de Pix.
    /// </summary>
    /// <param name="transactionId">Identificador da transação a ser liquidada.</param>
    public void SimulatePaymentSettlement(string transactionId)
    {
        _transactions[transactionId] = new PaymentStatusResult(transactionId, PaymentTransactionStatus.Paid, DateTime.UtcNow);
    }
}
