using FluentAssertions;
using Master.Application.Billing.Payments;
using Master.Domain.Billing;
using Master.Infrastructure.Payments;

namespace UnitTests.Backend.Billing;

/// <summary>
/// Testes unitários para o adaptador de gateway de pagamentos <see cref="InMemoryPaymentGateway"/>.
/// </summary>
public sealed class PaymentGatewayTests
{
    private readonly InMemoryPaymentGateway _gateway = new();

    /// <summary>
    /// Valida criação de cliente no gateway em memória.
    /// </summary>
    [Fact]
    public async Task GetOrCreateCustomerAsync_WithValidData_ShouldReturnCustomer()
    {
        // Arrange
        var request = new PaymentCustomerRequest(
            "Agência Performance LTDA",
            "12345678000195",
            "financeiro@performance.com.br",
            "11987654321");

        // Act
        var result = await _gateway.GetOrCreateCustomerAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CustomerId.Should().NotBeNullOrWhiteSpace();
        result.Value.Provider.Should().Be("InMemory");
    }

    /// <summary>
    /// Valida cobrança imediata bem-sucedida via cartão de crédito.
    /// </summary>
    [Fact]
    public async Task ChargeCreditCardAsync_WithValidCard_ShouldApproveCharge()
    {
        // Arrange
        var request = new CreditCardChargeRequest(
            "cus_123456",
            497.00m,
            "AdMetricsPro - Plano Pro Mensal",
            "ref_tx_001",
            "Carlos Silva",
            "4111111111111111",
            "12",
            "2030",
            "123");

        // Act
        var result = await _gateway.ChargeCreditCardAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsSuccess.Should().BeTrue();
        result.Value.TransactionId.Should().StartWith("tx_mock_");
        result.Value.AuthorizationCode.Should().NotBeNullOrWhiteSpace();
        result.Value.ErrorMessage.Should().BeNull();
    }

    /// <summary>
    /// Valida que cartão simulando recusa (ex: final 0002) retorna falha sem lançar exception.
    /// </summary>
    [Fact]
    public async Task ChargeCreditCardAsync_WithDeclinedCard_ShouldReturnBusinessFailure()
    {
        // Arrange
        var request = new CreditCardChargeRequest(
            "cus_123456",
            497.00m,
            "AdMetricsPro - Plano Pro Mensal",
            "ref_tx_002",
            "Carlos Silva",
            "4111111111110002", // Simulação de recusa
            "12",
            "2030",
            "123");

        // Act
        var result = await _gateway.ChargeCreditCardAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsSuccess.Should().BeFalse();
        result.Value.ErrorMessage.Should().Contain("recusada");
    }

    /// <summary>
    /// Valida geração de cobrança instantânea via Pix com QR Code e Copia e Cola.
    /// </summary>
    [Fact]
    public async Task CreatePixChargeAsync_WithValidRequest_ShouldReturnQrCodeAndPayload()
    {
        // Arrange
        var request = new PixChargeRequest(
            "cus_123456",
            4771.20m,
            "AdMetricsPro - Plano Pro Anual",
            "ref_tx_003");

        // Act
        var result = await _gateway.CreatePixChargeAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var pix = result.Value;
        pix.TransactionId.Should().StartWith("pix_mock_");
        pix.QrCodeBase64.Should().NotBeNullOrWhiteSpace();
        pix.CopiaECola.Should().StartWith("00020126580014br.gov.bcb.pix");
        pix.ExpiresAtUtc.Should().BeAfter(DateTime.UtcNow);
    }

    /// <summary>
    /// Valida consulta de status da transação no gateway.
    /// </summary>
    [Fact]
    public async Task GetPaymentStatusAsync_AfterSimulatingPayment_ShouldReturnPaid()
    {
        // Arrange
        var pixRequest = new PixChargeRequest("cus_123456", 100m, "Teste", "ref_tx_004");
        var pixResult = await _gateway.CreatePixChargeAsync(pixRequest);
        var txId = pixResult.Value.TransactionId;

        // Act: Simular liquidação
        _gateway.SimulatePaymentSettlement(txId);
        var statusResult = await _gateway.GetPaymentStatusAsync(txId);

        // Assert
        statusResult.IsSuccess.Should().BeTrue();
        statusResult.Value.Status.Should().Be(PaymentTransactionStatus.Paid);
        statusResult.Value.PaidAtUtc.Should().NotBeNull();
    }
}
