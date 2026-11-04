using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SocialPublishing.Application.Brand.DTOs;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Brand;
using UnitTests.Frontend.Common;
using WebApp.Components.Social;
using WebApp.Services.Social;
using Xunit;

namespace UnitTests.Frontend.Social;

/// <summary>
/// Testes bUnit do formulário <see cref="BrandVoiceForm"/>.
/// </summary>
public sealed class BrandVoiceFormTests : BunitTestBase
{
    private readonly ISocialPublishingClient _socialClient = Substitute.For<ISocialPublishingClient>();

    /// <summary>
    /// Registra mock do cliente social.
    /// </summary>
    public BrandVoiceFormTests()
    {
        Services.AddSingleton(_socialClient);
    }

    /// <summary>
    /// Soma dos pesos diferente de 100 desabilita salvar e não chama upsert.
    /// </summary>
    [Fact]
    public async Task Form_WhenWeightsSumNot100_ShouldDisableSaveAndNotCallUpsert()
    {
        var workspaceId = Guid.NewGuid();
        _socialClient.GetBrandVoiceAsync(workspaceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<BrandVoiceDto>.Failure(SocialErrors.BrandVoiceNotFound)));

        var cut = Render<BrandVoiceForm>(p => p.Add(x => x.WorkspaceId, workspaceId));
        cut.WaitForAssertion(() => cut.Find("[data-testid='brand-voice-weight-authority']").Should().NotBeNull());

        await cut.Find("[data-testid='brand-voice-weight-sale']").ChangeAsync("30");

        cut.Find("[data-testid='brand-voice-save']").HasAttribute("disabled").Should().BeTrue();
        await cut.Find("[data-testid='brand-voice-save']").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        await _socialClient.DidNotReceive().UpsertBrandVoiceAsync(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<EmojiPolicy>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Soma 100 envia todos os campos ao upsert.
    /// </summary>
    [Fact]
    public async Task Form_WhenWeightsSum100_ShouldCallUpsertWithFields()
    {
        var workspaceId = Guid.NewGuid();
        _socialClient.GetBrandVoiceAsync(workspaceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<BrandVoiceDto>.Failure(SocialErrors.BrandVoiceNotFound)));

        _socialClient.UpsertBrandVoiceAsync(
                workspaceId,
                "Tom teste",
                "Persona teste",
                Arg.Any<IReadOnlyList<string>>(),
                Arg.Any<IReadOnlyList<string>>(),
                EmojiPolicy.Sparingly,
                20,
                40,
                20,
                25,
                15,
                "pt-BR",
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<BrandVoiceDto>.Success(CreateDto(workspaceId))));

        var cut = Render<BrandVoiceForm>(p => p.Add(x => x.WorkspaceId, workspaceId));
        cut.WaitForAssertion(() => cut.Find("[data-testid='brand-voice-tone']").Should().NotBeNull());

        await cut.Find("[data-testid='brand-voice-tone']").ChangeAsync("Tom teste");
        await cut.Find("[data-testid='brand-voice-persona']").ChangeAsync("Persona teste");
        await cut.Find("[data-testid='brand-voice-weight-authority']").ChangeAsync("40");
        await cut.Find("[data-testid='brand-voice-weight-sale']").ChangeAsync("20");
        await cut.Find("[data-testid='brand-voice-weight-connection']").ChangeAsync("25");
        await cut.Find("[data-testid='brand-voice-weight-social-proof']").ChangeAsync("15");

        await cut.Find("[data-testid='brand-voice-save']").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        await _socialClient.Received(1).UpsertBrandVoiceAsync(
            workspaceId,
            "Tom teste",
            "Persona teste",
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<IReadOnlyList<string>>(),
            EmojiPolicy.Sparingly,
            20,
            40,
            20,
            25,
            15,
            "pt-BR",
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Perfil existente preenche os campos após GET.
    /// </summary>
    [Fact]
    public void Form_WhenProfileExists_ShouldPopulateFields()
    {
        var workspaceId = Guid.NewGuid();
        var dto = CreateDto(workspaceId) with { Tone = "Tom persistido", Persona = "Persona persistida" };
        _socialClient.GetBrandVoiceAsync(workspaceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<BrandVoiceDto>.Success(dto)));

        var cut = Render<BrandVoiceForm>(p => p.Add(x => x.WorkspaceId, workspaceId));
        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='brand-voice-tone']").GetAttribute("value").Should().Be("Tom persistido");
            cut.Find("[data-testid='brand-voice-persona']").GetAttribute("value").Should().Be("Persona persistida");
        });
    }

    /// <summary>
    /// BrandVoiceNotFound deixa formulário vazio sem alerta de erro.
    /// </summary>
    [Fact]
    public void Form_WhenBrandVoiceNotFound_ShouldShowEmptyFormWithoutError()
    {
        var workspaceId = Guid.NewGuid();
        _socialClient.GetBrandVoiceAsync(workspaceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<BrandVoiceDto>.Failure(SocialErrors.BrandVoiceNotFound)));

        var cut = Render<BrandVoiceForm>(p => p.Add(x => x.WorkspaceId, workspaceId));
        cut.WaitForAssertion(() => cut.Find("[data-testid='brand-voice-tone']").Should().NotBeNull());

        cut.FindAll("[data-testid='brand-voice-load-error']").Count.Should().Be(0);
        cut.Find("[data-testid='brand-voice-tone']").GetAttribute("value").Should().BeEmpty();
    }

    /// <summary>
    /// Falha no PUT exibe erro e mantém o tom digitado.
    /// </summary>
    [Fact]
    public async Task Form_WhenUpsertFails_ShouldShowErrorAndKeepInput()
    {
        var workspaceId = Guid.NewGuid();
        _socialClient.GetBrandVoiceAsync(workspaceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<BrandVoiceDto>.Failure(SocialErrors.BrandVoiceNotFound)));

        _socialClient.UpsertBrandVoiceAsync(
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IReadOnlyList<string>>(),
                Arg.Any<IReadOnlyList<string>>(),
                Arg.Any<EmojiPolicy>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<BrandVoiceDto>.Failure(
                Error.Validation("Social.PillarWeightsInvalid", "Soma inválida no servidor."))));

        var cut = Render<BrandVoiceForm>(p => p.Add(x => x.WorkspaceId, workspaceId));
        cut.WaitForAssertion(() => cut.Find("[data-testid='brand-voice-tone']").Should().NotBeNull());

        await cut.Find("[data-testid='brand-voice-tone']").ChangeAsync("Tom que permanece");
        await cut.Find("[data-testid='brand-voice-save']").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='brand-voice-save-error']").TextContent.Should().Contain("Soma inválida");
            cut.Find("[data-testid='brand-voice-tone']").GetAttribute("value").Should().Be("Tom que permanece");
        });
    }

    private static BrandVoiceDto CreateDto(Guid workspaceId) =>
        new(
            Guid.NewGuid(),
            workspaceId,
            "Tom",
            "Persona",
            Array.Empty<string>(),
            Array.Empty<string>(),
            EmojiPolicy.Sparingly,
            20,
            40,
            20,
            25,
            15,
            "pt-BR",
            DateTime.UtcNow);
}
