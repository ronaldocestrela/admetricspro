using BuildingBlocks.Application.Persistence;
using FluentAssertions;
using Master.Application.Billing.Checkout.Commands.ProcessCheckout;
using Master.Application.Billing.Payments;
using Master.Application.Repositories;
using Master.Domain.Billing;
using Master.Domain.Plans;
using Master.Domain.Tenants;
using NSubstitute;

namespace UnitTests.Backend.Billing;

/// <summary>
/// Testes unitários para o processamento de checkout de assinatura (<see cref="ProcessCheckoutCommandHandler"/>).
/// </summary>
public sealed class ProcessCheckoutCommandHandlerTests
{
    private readonly ITenantRepository _tenantRepository = Substitute.For<ITenantRepository>();
    private readonly IPlanRepository _planRepository = Substitute.For<IPlanRepository>();
    private readonly ITenantPaymentTransactionRepository _transactionRepository = Substitute.For<ITenantPaymentTransactionRepository>();
    private readonly IPaymentGatewayService _paymentGateway = Substitute.For<IPaymentGatewayService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly SubscriptionPlan _sampleProPlan = SubscriptionPlan.Create(
        "Plano Pro",
        "Para agências em expansão",
        SubscriptionTier.Pro,
        monthlyPrice: 497.00m,
        annualDiscountPercentage: 20,
        PlanLimits.Create(15, 10, 250_000m).Value,
        PlanFeatures.Default()).Value;

    /// <summary>
    /// Valida que cartão aprovado ativa o tenant e persiste a transação como Paid.
    /// </summary>
    [Fact]
    public async Task Handle_WithCreditCardApproved_ShouldActivateTenantAndPersistPaidTransaction()
    {
        // Arrange
        var tenant = Tenant.Create("Agência Alfa", "12345678000190", "agencia-alfa", SubscriptionTier.Trial).Value;
        _tenantRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>())
            .Returns(tenant);

        _planRepository.GetByTierAsync(SubscriptionTier.Pro, Arg.Any<CancellationToken>())
            .Returns(_sampleProPlan);

        _paymentGateway.GetOrCreateCustomerAsync(Arg.Any<PaymentCustomerRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentGatewayCustomer("cus_123", "Asaas"));

        _paymentGateway.ChargeCreditCardAsync(Arg.Any<CreditCardChargeRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CreditCardChargeResult("tx_card_999", IsSuccess: true, AuthorizationCode: "AUTH_OK"));

        var handler = new ProcessCheckoutCommandHandler(
            _tenantRepository,
            _planRepository,
            _transactionRepository,
            _paymentGateway,
            _unitOfWork);

        var command = new ProcessCheckoutCommand(
            tenant.Id.Value,
            SubscriptionTier.Pro,
            "Monthly",
            PaymentMethod.CreditCard,
            CardHolderName: "Carlos Silva",
            CardNumber: "4111111111111111",
            ExpiryMonth: "12",
            ExpiryYear: "2030",
            Ccv: "123");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsActivated.Should().BeTrue();
        result.Value.Status.Should().Be(PaymentTransactionStatus.Paid);
        result.Value.Amount.Should().Be(497.00m);

        tenant.Status.Should().Be(TenantStatus.Active);
        tenant.Tier.Should().Be(SubscriptionTier.Pro);

        await _transactionRepository.Received(1).AddAsync(Arg.Is<TenantPaymentTransaction>(t => t.Status == PaymentTransactionStatus.Paid), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que cartão recusado persiste transação como Failed e não ativa o tenant.
    /// </summary>
    [Fact]
    public async Task Handle_WithCreditCardDeclined_ShouldPersistFailedTransactionAndNotActivateTenant()
    {
        // Arrange
        var tenant = Tenant.Create("Agência Alfa", "12345678000190", "agencia-alfa", SubscriptionTier.Trial).Value;
        _tenantRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>())
            .Returns(tenant);

        _planRepository.GetByTierAsync(SubscriptionTier.Pro, Arg.Any<CancellationToken>())
            .Returns(_sampleProPlan);

        _paymentGateway.GetOrCreateCustomerAsync(Arg.Any<PaymentCustomerRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentGatewayCustomer("cus_123", "Asaas"));

        _paymentGateway.ChargeCreditCardAsync(Arg.Any<CreditCardChargeRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CreditCardChargeResult("tx_card_fail", IsSuccess: false, ErrorMessage: "Cartão recusado."));

        var handler = new ProcessCheckoutCommandHandler(
            _tenantRepository,
            _planRepository,
            _transactionRepository,
            _paymentGateway,
            _unitOfWork);

        var command = new ProcessCheckoutCommand(
            tenant.Id.Value,
            SubscriptionTier.Pro,
            "Monthly",
            PaymentMethod.CreditCard,
            CardHolderName: "Carlos Silva",
            CardNumber: "4111111111110002",
            ExpiryMonth: "12",
            ExpiryYear: "2030",
            Ccv: "123");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsActivated.Should().BeFalse();
        result.Value.Status.Should().Be(PaymentTransactionStatus.Failed);
        result.Value.FailureReason.Should().Be("Cartão recusado.");

        tenant.Status.Should().Be(TenantStatus.Active); // Mantém o status anterior sem alterar para paid pro
        tenant.Tier.Should().Be(SubscriptionTier.Trial);

        await _transactionRepository.Received(1).AddAsync(Arg.Is<TenantPaymentTransaction>(t => t.Status == PaymentTransactionStatus.Failed), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que checkout via Pix retorna QR Code e mantém a transação como Pending.
    /// </summary>
    [Fact]
    public async Task Handle_WithPix_ShouldReturnQrCodeAndKeepTransactionPending()
    {
        // Arrange
        var tenant = Tenant.Create("Agência Alfa", "12345678000190", "agencia-alfa", SubscriptionTier.Trial).Value;
        _tenantRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>())
            .Returns(tenant);

        _planRepository.GetByTierAsync(SubscriptionTier.Pro, Arg.Any<CancellationToken>())
            .Returns(_sampleProPlan);

        _paymentGateway.GetOrCreateCustomerAsync(Arg.Any<PaymentCustomerRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentGatewayCustomer("cus_123", "Asaas"));

        _paymentGateway.CreatePixChargeAsync(Arg.Any<PixChargeRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PixChargeResult("pix_tx_123", "base64-qr", "0002012658...pix", DateTime.UtcNow.AddHours(24)));

        var handler = new ProcessCheckoutCommandHandler(
            _tenantRepository,
            _planRepository,
            _transactionRepository,
            _paymentGateway,
            _unitOfWork);

        var command = new ProcessCheckoutCommand(
            tenant.Id.Value,
            SubscriptionTier.Pro,
            "Annual",
            PaymentMethod.Pix);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsActivated.Should().BeFalse();
        result.Value.Status.Should().Be(PaymentTransactionStatus.Pending);
        result.Value.PaymentMethod.Should().Be(PaymentMethod.Pix);
        result.Value.PixQrCode.Should().Be("base64-qr");
        result.Value.PixCopiaECola.Should().Be("0002012658...pix");
        result.Value.Amount.Should().Be(4771.20m);

        tenant.Tier.Should().Be(SubscriptionTier.Trial); // Permanece Trial até confirmação do Pix

        await _transactionRepository.Received(1).AddAsync(Arg.Is<TenantPaymentTransaction>(t => t.Status == PaymentTransactionStatus.Pending), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
