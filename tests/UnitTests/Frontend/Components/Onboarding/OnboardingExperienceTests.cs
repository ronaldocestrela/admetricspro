using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Master.Application.Tenants.Queries.CheckSubdomainAvailability;
using Master.Application.Tenants.Queries.CheckTaxDocumentAvailability;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using UnitTests.Frontend.Common;
using WebApp.Components.Onboarding;
using WebApp.Components.Landing;
using WebApp.Models;
using WebApp.Services;

namespace UnitTests.Frontend.Components.Onboarding;

/// <summary>Contratos visuais e comportamentais do cadastro com a identidade da LP.</summary>
public sealed class OnboardingExperienceTests : BunitTestBase
{
    private readonly ITenantOnboardingClientService _client = Substitute.For<ITenantOnboardingClientService>();

    /// <summary>Registra o cliente de validação para respostas controladas.</summary>
    public OnboardingExperienceTests() => Services.AddSingleton(_client);

    /// <summary>Exibe verificação de CPF e troca o estado apenas ao receber a resposta.</summary>
    [Fact]
    public async Task Document_ShouldShowSpinnerUntilCpfIsVerified()
    {
        var response = new TaskCompletionSource<Result<TaxDocumentAvailabilityResponse>>();
        _client.CheckTaxDocumentAvailabilityAsync("52998224725", Arg.Any<CancellationToken>()).Returns(response.Task);
        var cut = Render<StepCompanyInfo>(p => p.Add(x => x.Model, new TenantOnboardingFormModel()));
        var inputTask = cut.Find("#company-cnpj").InputAsync(new ChangeEventArgs { Value = "52998224725" });
        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='document-validation']").GetAttribute("data-state").Should().Be("checking");
            cut.Find(".validation-spinner").Should().NotBeNull();
            cut.Find("[data-testid='document-validation']").TextContent.Should().Contain("Verificando CPF");
        });
        response.SetResult(Result<TaxDocumentAvailabilityResponse>.Success(new("52998224725", "529.982.247-25", true, true, "CPF")));
        await inputTask;
        cut.Find("[data-testid='document-validation']").GetAttribute("data-state").Should().Be("success");
        cut.Find(".validation-check").Should().NotBeNull();
        cut.Find("[data-testid='document-validation']").TextContent.Should().Contain("CPF válido");
    }

    /// <summary>Mostra o endereço somente após verificar disponibilidade e o remove ao editar.</summary>
    [Fact]
    public async Task Subdomain_ShouldShowVerifiedAccessPreviewAndIgnoreStaleResponse()
    {
        var first = new TaskCompletionSource<Result<SubdomainAvailabilityResponse>>();
        var second = new TaskCompletionSource<Result<SubdomainAvailabilityResponse>>();
        _client.CheckSubdomainAvailabilityAsync("primeira", Arg.Any<CancellationToken>()).Returns(first.Task);
        _client.CheckSubdomainAvailabilityAsync("segunda", Arg.Any<CancellationToken>()).Returns(second.Task);
        var cut = Render<StepSubdomainIdentity>(p => p.Add(x => x.Model, new TenantOnboardingFormModel()));
        var firstTask = cut.Find("#subdomain-input").InputAsync(new ChangeEventArgs { Value = "primeira" });
        cut.WaitForAssertion(() => cut.Find("[data-testid='subdomain-validation']").TextContent.Should().Contain("Verificando subdomínio"));
        var secondTask = cut.Find("#subdomain-input").InputAsync(new ChangeEventArgs { Value = "segunda" });
        second.SetResult(Result<SubdomainAvailabilityResponse>.Success(new("segunda", true)));
        await secondTask;
        cut.Find("[data-testid='subdomain-access-preview']").TextContent.Should().Contain("https://segunda.admetricspro.com.br");
        first.SetResult(Result<SubdomainAvailabilityResponse>.Success(new("primeira", false, "Já está em uso")));
        await firstTask;
        cut.Find("[data-testid='subdomain-access-preview']").TextContent.Should().Contain("segunda");
        cut.Find("[data-testid='subdomain-validation']").GetAttribute("data-state").Should().Be("success");
        cut.Find("#subdomain-input").Input("ab");
        cut.FindAll("[data-testid='subdomain-access-preview']").Should().BeEmpty();
    }

    /// <summary>Marca e desmarca os cinco requisitos durante a digitação e alterna a visibilidade.</summary>
    [Fact]
    public void Admin_ShouldTrackPasswordRulesAndToggleVisibility()
    {
        var model = new TenantOnboardingFormModel();
        var cut = Render<StepAdminAccount>(p => p.Add(x => x.Model, model));
        cut.FindAll(".password-rule").Should().HaveCount(5);
        cut.Find("#admin-password").Input("Abcdef1!");
        model.AdminPassword.Should().Be("Abcdef1!");
        cut.FindAll(".password-rule.is-met").Should().HaveCount(5);
        cut.Find("button[aria-controls='admin-password']").Click();
        cut.Find("#admin-password").GetAttribute("type").Should().Be("text");
        cut.Find("button[aria-controls='admin-password']").GetAttribute("aria-label").Should().Be("Ocultar senha");
        cut.Find("button[aria-controls='admin-password']").Click();
        cut.Find("#admin-password").GetAttribute("type").Should().Be("password");
        cut.Find("#admin-password").Input("abc");
        cut.FindAll(".password-rule.is-met").Should().ContainSingle();
        cut.Find(".password-rule.is-met").GetAttribute("data-rule").Should().Be("lowercase");
    }

    /// <summary>Prioriza o período grátis e permite selecionar plano sem informar pagamento.</summary>
    [Fact]
    public void Plans_ShouldPrioritizeTrialAndUseKeyboardAccessibleSelection()
    {
        var model = new TenantOnboardingFormModel();
        var cut = Render<StepPlanSelection>(p => p.Add(x => x.Model, model));
        cut.Find(".trial-guarantee").TextContent.Should().Contain("14 dias").And.Contain("cartão");
        cut.Find(".trial-guarantee h2").TextContent.Should().Contain("14 dias");
        cut.FindAll(".pricing-card").Should().HaveCount(3);
        cut.Find("button[data-plan='Starter']").Click();
        model.Tier.Should().Be(Master.Domain.Tenants.SubscriptionTier.Starter);
        cut.Find("button[data-plan='Starter']").GetAttribute("aria-pressed").Should().Be("true");
        cut.FindAll("input").Should().BeEmpty();
    }

    /// <summary>Deixa os valores apenas no cadastro e mantém os planos comparáveis na LP.</summary>
    [Fact]
    public void LandingPlans_ShouldLinkToSignupWithoutDisplayingPrices()
    {
        var cut = Render<PricingSection>();
        cut.Markup.Should().NotContain("R$ 197").And.NotContain("R$ 497").And.NotContain("R$ 1.497");
        cut.FindAll(".pricing-card a").Should().HaveCount(3);
    }
}
