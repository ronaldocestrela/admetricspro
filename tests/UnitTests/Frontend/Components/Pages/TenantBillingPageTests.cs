using AngleSharp.Dom;
using Bunit;
using FluentAssertions;
using Master.Application.Billing.Checkout.Commands.ProcessCheckout;
using Master.Application.Billing.Checkout.Queries.GetCheckoutPreview;
using Master.Domain.Billing;
using Master.Domain.Tenants;
using NSubstitute;
using Tenants.Application.Auth.DTOs;
using UnitTests.Frontend.Common;
using WebApp.Components.Pages;
using WebApp.State;
using Xunit;

namespace UnitTests.Frontend.Components.Pages;

/// <summary>
/// Testes unitários com bUnit para a página de faturamento e checkout de assinatura (<see cref="TenantBillingPage"/>).
/// Valida exibição dos planos, alternância de ciclo de faturamento, cálculo de preview de descontos,
/// checkout via cartão de crédito com ativação imediata e geração de Pix.
/// </summary>
public sealed class TenantBillingPageTests : BunitTestBase
{
    private readonly Guid _tenantId = Guid.NewGuid();

    /// <summary>
    /// Inicializa o contexto com um usuário autenticado em regime de trial.
    /// </summary>
    public TenantBillingPageTests()
    {
        var session = new AuthenticatedTenantUserDto(
            AccessToken: "test-token",
            TokenType: "Bearer",
            ExpiresIn: 3600,
            UserId: Guid.NewGuid(),
            Email: "diretoria@agenciaalfa.com.br",
            FullName: "Diretor Comercial",
            Role: "Owner",
            TenantId: _tenantId,
            Subdomain: "agenciaalfa",
            Branding: null);

        TenantSessionStateProvider.SetSession(session);

        // Mock default preview
        BillingClientService.GetCheckoutPreviewAsync(
            _tenantId,
            SubscriptionTier.Pro,
            "Monthly",
            Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result<CheckoutPreviewDto>.Success(new CheckoutPreviewDto(
                Tier: SubscriptionTier.Pro,
                PlanName: "Plano Pro",
                BillingCycle: "Monthly",
                BaseMonthlyPrice: 497m,
                DiscountPercentage: 0,
                TotalPayableNow: 497m,
                SavingsAmount: 0m,
                NextRenewalDateUtc: DateTime.UtcNow.AddMonths(1))));

        BillingClientService.GetCheckoutPreviewAsync(
            _tenantId,
            SubscriptionTier.Pro,
            "Annual",
            Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result<CheckoutPreviewDto>.Success(new CheckoutPreviewDto(
                Tier: SubscriptionTier.Pro,
                PlanName: "Plano Pro",
                BillingCycle: "Annual",
                BaseMonthlyPrice: 497m,
                DiscountPercentage: 20,
                TotalPayableNow: 4771.20m,
                SavingsAmount: 1192.80m,
                NextRenewalDateUtc: DateTime.UtcNow.AddYears(1))));
    }

    /// <summary>
    /// Valida que a página de billing renderiza os planos, cabeçalho de trial e alternador de ciclo.
    /// </summary>
    [Fact]
    public void TenantBillingPage_ShouldRenderPlansAndBillingCycleToggle()
    {
        // Act
        var cut = Render<TenantBillingPage>();

        // Assert
        cut.Find(".billing-page-title").TextContent.Should().Contain("Assinatura & Planos");
        cut.Find(".plan-card-starter").Should().NotBeNull();
        cut.Find(".plan-card-pro").Should().NotBeNull();
        cut.Find(".plan-card-enterprise").Should().NotBeNull();
        cut.Find(".cycle-toggle-container").Should().NotBeNull();
    }

    /// <summary>
    /// Valida que ao alternar o ciclo para anual, recalcula o preview e exibe o desconto e economia.
    /// </summary>
    [Fact]
    public void TenantBillingPage_WhenCycleSwitchedToAnnual_ShouldUpdatePreviewAndSavings()
    {
        // Act
        var cut = Render<TenantBillingPage>();

        var annualButton = cut.Find("button.btn-cycle-annual");
        annualButton.Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var summaryPrice = cut.Find(".checkout-summary-total");
            summaryPrice.TextContent.Should().Contain("4.771,20");
            cut.Find(".discount-badge").TextContent.Should().Contain("20% OFF");
        });
    }

    /// <summary>
    /// Valida que ao preencher os dados de cartão e submeter, ativa o plano e remove status de trial do tenant.
    /// </summary>
    [Fact]
    public void TenantBillingPage_WhenCreditCardSubmitted_ShouldActivateSubscription()
    {
        // Arrange
        var checkoutResult = new ProcessCheckoutResult(
            TransactionId: Guid.NewGuid(),
            TenantId: _tenantId,
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

        BillingClientService.ProcessCheckoutAsync(Arg.Any<ProcessCheckoutCommand>(), Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result<ProcessCheckoutResult>.Success(checkoutResult));

        var cut = Render<TenantBillingPage>();

        // Act
        cut.Find("input#card-holder-name").Change("Carlos Silva");
        cut.Find("input#card-number").Change("5555444433332222");
        cut.Find("input#card-expiry-month").Change("12");
        cut.Find("input#card-expiry-year").Change("2029");
        cut.Find("input#card-ccv").Change("123");

        var submitBtn = cut.Find("button#btn-submit-checkout");
        submitBtn.Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find(".activation-success-banner").Should().NotBeNull();
            cut.Find(".activation-success-banner").TextContent.Should().Contain("Assinatura Ativada com Sucesso");
            TenantStateProvider.CurrentTenant.IsTrial.Should().BeFalse();
        });
    }

    /// <summary>
    /// Valida que ao selecionar a forma de pagamento Pix e gerar, exibe o QR Code e código Copia e Cola.
    /// </summary>
    [Fact]
    public void TenantBillingPage_WhenPixSelectedAndGenerated_ShouldDisplayPixQrCodeAndCopiaECola()
    {
        // Arrange
        var pixResult = new ProcessCheckoutResult(
            TransactionId: Guid.NewGuid(),
            TenantId: _tenantId,
            Tier: SubscriptionTier.Pro,
            BillingCycle: "Monthly",
            PaymentMethod: PaymentMethod.Pix,
            Amount: 497m,
            Status: PaymentTransactionStatus.Pending,
            IsActivated: false,
            PixQrCode: "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAA",
            PixCopiaECola: "00020126360014BR.GOV.BCB.PIX0114+55119999999995204000053039865405497.005802BR5913AdMetricsPro6009SAO PAULO62070503***6304ABCD",
            PixExpiresAtUtc: DateTime.UtcNow.AddMinutes(30),
            FailureReason: null);

        BillingClientService.ProcessCheckoutAsync(Arg.Any<ProcessCheckoutCommand>(), Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result<ProcessCheckoutResult>.Success(pixResult));

        var cut = Render<TenantBillingPage>();

        // Act - Seleciona Pix
        var pixMethodBtn = cut.Find("button#btn-method-pix");
        pixMethodBtn.Click();

        // Gera o Pix
        var generatePixBtn = cut.Find("button#btn-submit-checkout");
        generatePixBtn.Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find(".pix-qrcode-container").Should().NotBeNull();
            cut.Find(".pix-copia-cola-input").GetAttribute("value").Should().Contain("00020126360014BR.GOV.BCB.PIX");
        });
    }

    /// <summary>
    /// Valida que ao ocorrer erro no processamento do checkout, exibe o banner de alerta com a mensagem de erro.
    /// </summary>
    [Fact]
    public void TenantBillingPage_WhenCheckoutFails_ShouldDisplayErrorMessage()
    {
        // Arrange
        BillingClientService.ProcessCheckoutAsync(Arg.Any<ProcessCheckoutCommand>(), Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result<ProcessCheckoutResult>.Failure(
                BuildingBlocks.Domain.Primitives.Error.Validation("Payment.CardRefused", "Transação recusada pela operadora do cartão.")));

        var cut = Render<TenantBillingPage>();

        // Act
        var submitBtn = cut.Find("button#btn-submit-checkout");
        submitBtn.Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var errorBanner = cut.Find(".checkout-error-alert");
            errorBanner.Should().NotBeNull();
            errorBanner.TextContent.Should().Contain("Transação recusada pela operadora do cartão.");
        });
    }
}
