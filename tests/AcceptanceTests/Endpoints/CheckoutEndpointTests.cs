using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Master.Application.Billing.Payments;
using Master.Application.Repositories;
using Master.Domain.Billing;
using Master.Domain.Plans;
using Master.Domain.Tenants;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using WebApi.Models;
using Xunit;

namespace AcceptanceTests.Endpoints;

/// <summary>
/// Testes de aceitação para os endpoints de checkout e ciclo de vida de faturamento (API REST /api/v1/billing/checkout).
/// </summary>
public sealed class CheckoutEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Inicializa uma nova instância de <see cref="CheckoutEndpointTests"/>.
    /// </summary>
    /// <param name="factory">Fábrica da aplicação web.</param>
    public CheckoutEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Valida que GET /api/v1/billing/checkout/preview calcula os valores líquidos corretamente com desconto anual.
    /// </summary>
    [Fact]
    public async Task GetCheckoutPreview_WithAnnualCycle_ShouldReturnOkWithDiscount()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var fakeTenantRepo = new FakeTenantRepo(tenantId);
                services.AddScoped<ITenantRepository>(_ => fakeTenantRepo);
            });
        }).CreateClient();

        // Act
        var response = await client.GetAsync($"/api/v1/billing/checkout/preview?tenantId={tenantId}&tier=Pro&billingCycle=Annual");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ResultEnvelope<CheckoutPreviewApiResponse>>(content, JsonOptions);

        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Tier.Should().Be(SubscriptionTier.Pro);
        result.Value.BillingCycle.Should().Be("Annual");
        result.Value.TotalPayableNow.Should().BeGreaterThan(0);
        result.Value.SavingsAmount.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Valida que POST /api/v1/billing/checkout com Cartão de Crédito aprova a cobrança e ativa a assinatura.
    /// </summary>
    [Fact]
    public async Task ProcessCheckout_WithCreditCard_ShouldReturnOkAndActivate()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var fakeTenantRepo = new FakeTenantRepo(tenantId);
                services.AddScoped<ITenantRepository>(_ => fakeTenantRepo);
            });
        }).CreateClient();

        var request = new ProcessCheckoutApiRequest(
            tenantId,
            SubscriptionTier.Pro,
            "Monthly",
            PaymentMethod.CreditCard,
            CardHolderName: "Roberto Lima",
            CardNumber: "4111111111111111",
            ExpiryMonth: "12",
            ExpiryYear: "2030",
            Ccv: "123");

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/billing/checkout", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ResultEnvelope<ProcessCheckoutApiResponse>>(content, JsonOptions);

        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.IsActivated.Should().BeTrue();
        result.Value.Status.Should().Be(PaymentTransactionStatus.Paid);
        result.Value.Amount.Should().Be(497.00m);
    }

    /// <summary>
    /// Valida que POST /api/v1/billing/checkout com Pix gera QR Code e chave Copia e Cola.
    /// </summary>
    [Fact]
    public async Task ProcessCheckout_WithPix_ShouldReturnOkWithQrCodeAndPendingStatus()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var fakeTenantRepo = new FakeTenantRepo(tenantId);
                services.AddScoped<ITenantRepository>(_ => fakeTenantRepo);
            });
        }).CreateClient();

        var request = new ProcessCheckoutApiRequest(
            tenantId,
            SubscriptionTier.Pro,
            "Annual",
            PaymentMethod.Pix);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/billing/checkout", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ResultEnvelope<ProcessCheckoutApiResponse>>(content, JsonOptions);

        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.IsActivated.Should().BeFalse();
        result.Value.Status.Should().Be(PaymentTransactionStatus.Pending);
        result.Value.PixQrCode.Should().NotBeNullOrWhiteSpace();
        result.Value.PixCopiaECola.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// Valida que GET /api/v1/billing/checkout/{id}/status retorna o status da transação.
    /// </summary>
    [Fact]
    public async Task GetPaymentStatus_WhenTransactionExists_ShouldReturnStatus()
    {
        // Arrange
        var txId = Guid.NewGuid();
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var fakeTxRepo = new FakeTxRepo(txId);
                services.AddScoped<ITenantPaymentTransactionRepository>(_ => fakeTxRepo);
            });
        }).CreateClient();

        // Act
        var response = await client.GetAsync($"/api/v1/billing/checkout/{txId}/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ResultEnvelope<PaymentStatusApiResponse>>(content, JsonOptions);

        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TransactionId.Should().NotBeEmpty();
        result.Value.Status.Should().Be(PaymentTransactionStatus.Pending);
    }

    /// <summary>
    /// Envelope de desserialização do padrão Result&lt;T&gt;.
    /// </summary>
    private sealed class ResultEnvelope<T>
    {
        public bool IsSuccess { get; set; }
        public bool IsFailure { get; set; }
        public T? Value { get; set; }
    }

    private sealed class FakeTenantRepo : ITenantRepository
    {
        private readonly Tenant _tenant;

        public FakeTenantRepo(Guid tenantId)
        {
            _tenant = Tenant.Create("Agência Teste", "12345678000195", "teste", SubscriptionTier.Trial).Value;
        }

        public Task AddAsync(Tenant entity, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Update(Tenant entity) { }

        public void Remove(Tenant entity) { }

        public Task<Tenant?> GetByIdAsync(TenantId id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Tenant?>(_tenant);

        public Task<Tenant?> GetBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default) =>
            Task.FromResult<Tenant?>(_tenant);

        public Task<Tenant?> GetByCnpjAsync(string cnpj, CancellationToken cancellationToken = default) =>
            Task.FromResult<Tenant?>(_tenant);

        public Task<Tenant?> GetByCustomDomainAsync(string customDomain, CancellationToken cancellationToken = default) =>
            Task.FromResult<Tenant?>(null);

        public Task<IReadOnlyList<Tenant>> GetTenantsForDunningEvaluationAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Tenant>>(new List<Tenant>());

        public Task<IReadOnlyList<Tenant>> GetTenantsForTrialNoticeEvaluationAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Tenant>>(new List<Tenant>());
    }

    private sealed class FakeTxRepo : ITenantPaymentTransactionRepository
    {
        private readonly Guid _txId;

        public FakeTxRepo(Guid txId)
        {
            _txId = txId;
        }

        public Task AddAsync(TenantPaymentTransaction transaction, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<TenantPaymentTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var tx = TenantPaymentTransaction.CreateCreditCard(
                TenantId.New(),
                SubscriptionTier.Pro,
                "Monthly",
                497.00m,
                "Asaas",
                "pay_123").Value;

            return Task.FromResult<TenantPaymentTransaction?>(tx);
        }

        public Task<TenantPaymentTransaction?> GetByGatewayTransactionIdAsync(string gatewayTransactionId, CancellationToken cancellationToken = default) =>
            Task.FromResult<TenantPaymentTransaction?>(null);

        public Task<IReadOnlyList<TenantPaymentTransaction>> GetByTenantIdAsync(TenantId tenantId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TenantPaymentTransaction>>(new List<TenantPaymentTransaction>());
    }
}
