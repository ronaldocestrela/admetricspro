using System.Net;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Master.Application.Billing.Checkout.Commands.ProcessCheckout;
using Master.Application.Billing.Checkout.Queries.GetCheckoutPreview;
using Master.Application.Billing.Checkout.Queries.GetPaymentStatus;
using Master.Domain.Billing;
using Master.Domain.Tenants;
using UnitTests.Frontend.Common;
using WebApp.Services;
using Xunit;

namespace UnitTests.Frontend.Services;

/// <summary>
/// Testes unitários para <see cref="BillingClientService"/>.
/// Valida comunicação HTTP desacoplada com a Web API para checkout, status e pré-visualização.
/// </summary>
public sealed class BillingClientServiceTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Valida que a pré-visualização de checkout invoca a rota correta e deserializa com sucesso.
    /// </summary>
    [Fact]
    public async Task GetCheckoutPreviewAsync_WhenApiReturnsSuccess_ShouldReturnPreviewDto()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var expectedDto = new CheckoutPreviewDto(
            Tier: SubscriptionTier.Pro,
            PlanName: "Plano Pro",
            BillingCycle: "Annual",
            BaseMonthlyPrice: 497m,
            DiscountPercentage: 20,
            TotalPayableNow: 4771.20m,
            SavingsAmount: 1192.80m,
            NextRenewalDateUtc: DateTime.UtcNow.AddYears(1));

        var apiResult = Result<CheckoutPreviewDto>.Success(expectedDto);

        var handler = new TestHttpMessageHandler((request, cancellationToken) =>
        {
            request.RequestUri!.PathAndQuery.Should().Contain($"/api/v1/billing/checkout/preview?tenantId={tenantId}&tier=Pro&billingCycle=Annual");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(apiResult, JsonOptions))
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost:7001") };
        var service = new BillingClientService(httpClient);

        // Act
        var result = await service.GetCheckoutPreviewAsync(tenantId, SubscriptionTier.Pro, "Annual");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Tier.Should().Be(SubscriptionTier.Pro);
        result.Value.DiscountPercentage.Should().Be(20);
        result.Value.TotalPayableNow.Should().Be(4771.20m);
    }

    /// <summary>
    /// Valida que ao falhar a chamada HTTP para preview, retorna falha tratada sem lançar exceção.
    /// </summary>
    [Fact]
    public async Task GetCheckoutPreviewAsync_WhenApiReturnsFailure_ShouldReturnFailureResult()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var apiResult = Result<CheckoutPreviewDto>.Failure(Error.NotFound("Tenant.NotFound", "Inquilino não encontrado."));

        var handler = new TestHttpMessageHandler((request, cancellationToken) =>
        {
            return new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(JsonSerializer.Serialize(apiResult, JsonOptions))
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost:7001") };
        var service = new BillingClientService(httpClient);

        // Act
        var result = await service.GetCheckoutPreviewAsync(tenantId, SubscriptionTier.Starter, "Monthly");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.NotFound");
    }

    /// <summary>
    /// Valida que o processamento de checkout com cartão de crédito bem-sucedido retorna a confirmação de ativação.
    /// </summary>
    [Fact]
    public async Task ProcessCheckoutAsync_WhenCreditCardSucceeds_ShouldReturnActivatedResult()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var command = new ProcessCheckoutCommand(
            TenantId: tenantId,
            Tier: SubscriptionTier.Pro,
            BillingCycle: "Monthly",
            PaymentMethod: PaymentMethod.CreditCard,
            CardHolderName: "Carlos Gestor",
            CardNumber: "5555444433332222",
            ExpiryMonth: "12",
            ExpiryYear: "2029",
            Ccv: "123");

        var expectedResult = new ProcessCheckoutResult(
            TransactionId: Guid.NewGuid(),
            TenantId: tenantId,
            Tier: SubscriptionTier.Pro,
            BillingCycle: "Monthly",
            PaymentMethod: PaymentMethod.CreditCard,
            Amount: 497m,
            Status: PaymentTransactionStatus.Paid,
            IsActivated: true,
            PixQrCode: null,
            PixCopiaECola: null,
            PixExpiresAtUtc: null,
            FailureReason: null);

        var apiResult = Result<ProcessCheckoutResult>.Success(expectedResult);

        var handler = new TestHttpMessageHandler((request, cancellationToken) =>
        {
            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri!.PathAndQuery.Should().Be("/api/v1/billing/checkout");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(apiResult, JsonOptions))
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost:7001") };
        var service = new BillingClientService(httpClient);

        // Act
        var result = await service.ProcessCheckoutAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsActivated.Should().BeTrue();
        result.Value.Status.Should().Be(PaymentTransactionStatus.Paid);
    }

    /// <summary>
    /// Valida que o processamento de checkout via Pix retorna as instruções de QR Code e payload copia e cola.
    /// </summary>
    [Fact]
    public async Task ProcessCheckoutAsync_WhenPixGenerated_ShouldReturnPendingWithPixData()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var command = new ProcessCheckoutCommand(
            TenantId: tenantId,
            Tier: SubscriptionTier.Pro,
            BillingCycle: "Annual",
            PaymentMethod: PaymentMethod.Pix);

        var expectedResult = new ProcessCheckoutResult(
            TransactionId: Guid.NewGuid(),
            TenantId: tenantId,
            Tier: SubscriptionTier.Pro,
            BillingCycle: "Annual",
            PaymentMethod: PaymentMethod.Pix,
            Amount: 4771.20m,
            Status: PaymentTransactionStatus.Pending,
            IsActivated: false,
            PixQrCode: "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAA",
            PixCopiaECola: "00020126360014BR.GOV.BCB.PIX0114+551199999999952040000530398654074771.205802BR5913AdMetricsPro6009SAO PAULO62070503***6304ABCD",
            PixExpiresAtUtc: DateTime.UtcNow.AddMinutes(30),
            FailureReason: null);

        var apiResult = Result<ProcessCheckoutResult>.Success(expectedResult);

        var handler = new TestHttpMessageHandler((request, cancellationToken) =>
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(apiResult, JsonOptions))
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost:7001") };
        var service = new BillingClientService(httpClient);

        // Act
        var result = await service.ProcessCheckoutAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(PaymentTransactionStatus.Pending);
        result.Value.PixQrCode.Should().NotBeNullOrEmpty();
        result.Value.PixCopiaECola.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Valida a consulta do status de pagamento de uma transação pendente.
    /// </summary>
    [Fact]
    public async Task GetPaymentStatusAsync_WhenPolled_ShouldReturnStatusDto()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var expectedDto = new PaymentStatusDto(
            TransactionId: transactionId,
            Status: PaymentTransactionStatus.Paid,
            IsPaid: true,
            PaidAtUtc: DateTime.UtcNow);

        var apiResult = Result<PaymentStatusDto>.Success(expectedDto);

        var handler = new TestHttpMessageHandler((request, cancellationToken) =>
        {
            request.RequestUri!.PathAndQuery.Should().Be($"/api/v1/billing/checkout/{transactionId}/status");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(apiResult, JsonOptions))
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost:7001") };
        var service = new BillingClientService(httpClient);

        // Act
        var result = await service.GetPaymentStatusAsync(transactionId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsPaid.Should().BeTrue();
        result.Value.Status.Should().Be(PaymentTransactionStatus.Paid);
    }
}
