using FluentAssertions;
using Master.Domain.Billing;
using Master.Domain.Tenants;

namespace UnitTests.Backend.Billing;

/// <summary>
/// Testes unitários para a entidade <see cref="TenantPaymentTransaction"/> e seu ciclo de vida.
/// </summary>
public sealed class TenantPaymentTransactionTests
{
    /// <summary>
    /// Valida a criação de uma transação de pagamento por cartão de crédito com status pendente.
    /// </summary>
    [Fact]
    public void CreateCreditCard_WithValidParameters_ShouldInitializeAsPending()
    {
        // Arrange
        var tenantId = TenantId.New();
        var amount = 497.00m;

        // Act
        var result = TenantPaymentTransaction.CreateCreditCard(
            tenantId,
            SubscriptionTier.Pro,
            "Monthly",
            amount,
            "Asaas");

        // Assert
        result.IsSuccess.Should().BeTrue();
        var tx = result.Value;
        tx.Id.Should().NotBeEmpty();
        tx.TenantId.Should().Be(tenantId);
        tx.Tier.Should().Be(SubscriptionTier.Pro);
        tx.BillingCycle.Should().Be("Monthly");
        tx.Amount.Should().Be(amount);
        tx.PaymentMethod.Should().Be(PaymentMethod.CreditCard);
        tx.Status.Should().Be(PaymentTransactionStatus.Pending);
        tx.GatewayProvider.Should().Be("Asaas");
        tx.PaidAtUtc.Should().BeNull();
        tx.PixQrCode.Should().BeNull();
    }

    /// <summary>
    /// Valida a criação de uma transação de pagamento via Pix com QR Code e expiração.
    /// </summary>
    [Fact]
    public void CreatePix_WithValidParameters_ShouldInitializeWithPixPayloads()
    {
        // Arrange
        var tenantId = TenantId.New();
        var amount = 4771.20m;
        var pixQrCode = "base64-qrcode-data";
        var pixCopiaECola = "00020126580014br.gov.bcb.pix...";
        var expiresAtUtc = DateTime.UtcNow.AddHours(24);

        // Act
        var result = TenantPaymentTransaction.CreatePix(
            tenantId,
            SubscriptionTier.Pro,
            "Annual",
            amount,
            "Asaas",
            pixQrCode,
            pixCopiaECola,
            expiresAtUtc);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var tx = result.Value;
        tx.PaymentMethod.Should().Be(PaymentMethod.Pix);
        tx.Status.Should().Be(PaymentTransactionStatus.Pending);
        tx.PixQrCode.Should().Be(pixQrCode);
        tx.PixCopiaECola.Should().Be(pixCopiaECola);
        tx.PixExpiresAtUtc.Should().Be(expiresAtUtc);
    }

    /// <summary>
    /// Valida rejeição quando o valor for menor ou igual a zero.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Create_WithInvalidAmount_ShouldFail(decimal invalidAmount)
    {
        // Act
        var result = TenantPaymentTransaction.CreateCreditCard(
            TenantId.New(),
            SubscriptionTier.Starter,
            "Monthly",
            invalidAmount,
            "Asaas");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PaymentTransaction.InvalidAmount");
    }

    /// <summary>
    /// Valida rejeição de plano Trial em transação financeira paga.
    /// </summary>
    [Fact]
    public void Create_WithTrialTier_ShouldFail()
    {
        // Act
        var result = TenantPaymentTransaction.CreateCreditCard(
            TenantId.New(),
            SubscriptionTier.Trial,
            "Monthly",
            97.00m,
            "Asaas");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PaymentTransaction.InvalidTier");
    }

    /// <summary>
    /// Valida transição de estado para pago com sucesso.
    /// </summary>
    [Fact]
    public void MarkAsPaid_WhenPending_ShouldUpdateStatusAndGatewayId()
    {
        // Arrange
        var tx = TenantPaymentTransaction.CreateCreditCard(
            TenantId.New(),
            SubscriptionTier.Pro,
            "Monthly",
            497.00m,
            "Asaas").Value;

        var paidAtUtc = DateTime.UtcNow;
        var gatewayTxId = "pay_asaas_123456";

        // Act
        var result = tx.MarkAsPaid(paidAtUtc, gatewayTxId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        tx.Status.Should().Be(PaymentTransactionStatus.Paid);
        tx.PaidAtUtc.Should().Be(paidAtUtc);
        tx.GatewayTransactionId.Should().Be(gatewayTxId);
    }

    /// <summary>
    /// Valida que marcar como pago uma transação já paga resulta em falha de conflito.
    /// </summary>
    [Fact]
    public void MarkAsPaid_WhenAlreadyPaid_ShouldFailWithConflict()
    {
        // Arrange
        var tx = TenantPaymentTransaction.CreateCreditCard(
            TenantId.New(),
            SubscriptionTier.Pro,
            "Monthly",
            497.00m,
            "Asaas").Value;

        tx.MarkAsPaid(DateTime.UtcNow, "pay_1");

        // Act
        var result = tx.MarkAsPaid(DateTime.UtcNow, "pay_2");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PaymentTransaction.AlreadyProcessed");
    }

    /// <summary>
    /// Valida transição de estado para falha com razão explicativa.
    /// </summary>
    [Fact]
    public void MarkAsFailed_WhenPending_ShouldUpdateStatusAndReason()
    {
        // Arrange
        var tx = TenantPaymentTransaction.CreateCreditCard(
            TenantId.New(),
            SubscriptionTier.Pro,
            "Monthly",
            497.00m,
            "Asaas").Value;

        // Act
        var result = tx.MarkAsFailed("Cartão recusado pelo emissor.");

        // Assert
        result.IsSuccess.Should().BeTrue();
        tx.Status.Should().Be(PaymentTransactionStatus.Failed);
        tx.FailureReason.Should().Be("Cartão recusado pelo emissor.");
    }
}
