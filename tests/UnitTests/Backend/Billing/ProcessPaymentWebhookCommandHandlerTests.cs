using BuildingBlocks.Application.Persistence;
using FluentAssertions;
using Master.Application.Billing.Checkout.Commands.ProcessWebhook;
using Master.Application.Billing.Payments;
using Master.Application.Repositories;
using Master.Domain.Billing;
using Master.Domain.Tenants;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace UnitTests.Backend.Billing;

/// <summary>
/// Testes unitários para o processamento de webhooks de gateway de pagamento (<see cref="ProcessPaymentWebhookCommandHandler"/>).
/// </summary>
public sealed class ProcessPaymentWebhookCommandHandlerTests
{
    private readonly ITenantRepository _tenantRepository = Substitute.For<ITenantRepository>();
    private readonly ITenantPaymentTransactionRepository _transactionRepository = Substitute.For<ITenantPaymentTransactionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IOptions<PaymentWebhookOptions> _options = Options.Create(new PaymentWebhookOptions
    {
        WebhookToken = "valid_secret_token_123"
    });

    /// <summary>
    /// Valida que evento PAYMENT_RECEIVED marca a transação como paga e ativa o tenant.
    /// </summary>
    [Fact]
    public async Task Handle_WhenPaymentReceived_ShouldMarkTransactionPaidAndActivateTenant()
    {
        // Arrange
        var tenant = Tenant.Create("Agência Pix", "12345678000195", "agencia-pix", SubscriptionTier.Trial).Value;
        var transaction = TenantPaymentTransaction.CreatePix(
            tenant.Id,
            SubscriptionTier.Pro,
            "Annual",
            4771.20m,
            "Asaas",
            "qr",
            "copia",
            DateTime.UtcNow.AddDays(1),
            gatewayTransactionId: "pay_asaas_888").Value;

        _transactionRepository.GetByGatewayTransactionIdAsync("pay_asaas_888", Arg.Any<CancellationToken>())
            .Returns(transaction);

        _tenantRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>())
            .Returns(tenant);

        var handler = new ProcessPaymentWebhookCommandHandler(
            _transactionRepository,
            _tenantRepository,
            _unitOfWork,
            _options);

        var command = new ProcessPaymentWebhookCommand(
            Provider: "Asaas",
            WebhookToken: "valid_secret_token_123",
            EventType: "PAYMENT_RECEIVED",
            GatewayTransactionId: "pay_asaas_888",
            Amount: 4771.20m,
            EventDateUtc: DateTime.UtcNow);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsProcessed.Should().BeTrue();
        result.Value.Status.Should().Be(PaymentTransactionStatus.Paid);

        transaction.Status.Should().Be(PaymentTransactionStatus.Paid);
        tenant.Status.Should().Be(TenantStatus.Active);
        tenant.Tier.Should().Be(SubscriptionTier.Pro);

        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que transação já liquidada é tratada de forma idempotente sem invocar commit redundante.
    /// </summary>
    [Fact]
    public async Task Handle_WhenTransactionAlreadyPaid_ShouldBeIdempotentWithoutCallingCommitAgain()
    {
        // Arrange
        var tenant = Tenant.Create("Agência Pix", "12345678000195", "agencia-pix", SubscriptionTier.Trial).Value;
        var transaction = TenantPaymentTransaction.CreatePix(
            tenant.Id,
            SubscriptionTier.Pro,
            "Annual",
            4771.20m,
            "Asaas",
            "qr",
            "copia",
            DateTime.UtcNow.AddDays(1),
            gatewayTransactionId: "pay_asaas_888").Value;

        transaction.MarkAsPaid(DateTime.UtcNow, "pay_asaas_888");

        _transactionRepository.GetByGatewayTransactionIdAsync("pay_asaas_888", Arg.Any<CancellationToken>())
            .Returns(transaction);

        var handler = new ProcessPaymentWebhookCommandHandler(
            _transactionRepository,
            _tenantRepository,
            _unitOfWork,
            _options);

        var command = new ProcessPaymentWebhookCommand(
            Provider: "Asaas",
            WebhookToken: "valid_secret_token_123",
            EventType: "PAYMENT_CONFIRMED",
            GatewayTransactionId: "pay_asaas_888",
            Amount: 4771.20m,
            EventDateUtc: DateTime.UtcNow);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsProcessed.Should().BeTrue();
        result.Value.IsAlreadyProcessed.Should().BeTrue();

        await _tenantRepository.DidNotReceive().GetByIdAsync(Arg.Any<TenantId>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que webhook com token inválido retorna erro de unauthorized.
    /// </summary>
    [Fact]
    public async Task Handle_WithInvalidWebhookToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var handler = new ProcessPaymentWebhookCommandHandler(
            _transactionRepository,
            _tenantRepository,
            _unitOfWork,
            _options);

        var command = new ProcessPaymentWebhookCommand(
            Provider: "Asaas",
            WebhookToken: "invalid_secret_token",
            EventType: "PAYMENT_RECEIVED",
            GatewayTransactionId: "pay_asaas_888",
            Amount: 100m,
            EventDateUtc: DateTime.UtcNow);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Webhook.Unauthorized");
    }
}
