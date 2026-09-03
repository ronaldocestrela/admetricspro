using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using UnitTests.Frontend.Common;
using WebApp.Components.Backoffice;

namespace UnitTests.Frontend.Components.Backoffice;

/// <summary>
/// Testes de componente bUnit para o Diálogo de Confirmação com Dupla Validação (<see cref="ConfirmActionDialog"/>).
/// Valida a trava de segurança para ações destrutivas, exigindo preenchimento de justificativa formal
/// e digitação de texto exato de confirmação antes de habilitar a ação.
/// </summary>
public class ConfirmActionDialogTests : BunitTestBase
{
    /// <summary>
    /// Valida que quando o diálogo está fechado (IsOpen = false), nada é renderizado no DOM.
    /// </summary>
    [Fact]
    public void ConfirmActionDialog_WhenIsOpenIsFalse_ShouldNotRenderModal()
    {
        // Act
        var cut = Render<ConfirmActionDialog>(parameters => parameters
            .Add(p => p.IsOpen, false)
            .Add(p => p.Title, "Suspender Tenant")
            .Add(p => p.ExpectedConfirmationText, "alphatech"));

        // Assert
        cut.FindAll(".modal-backdrop").Should().BeEmpty();
        cut.FindAll(".confirm-action-dialog").Should().BeEmpty();
    }

    /// <summary>
    /// Valida que quando o diálogo está aberto, renderiza o cabeçalho, mensagem de advertência e campos de formulário.
    /// </summary>
    [Fact]
    public void ConfirmActionDialog_WhenIsOpenIsTrue_ShouldRenderModalContent()
    {
        // Act
        var cut = Render<ConfirmActionDialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Title, "Confirmar Suspensão Forçada")
            .Add(p => p.Message, "Esta ação interromperá imediatamente o acesso da agência.")
            .Add(p => p.ExpectedConfirmationText, "alphatech"));

        // Assert
        cut.Find(".confirm-action-dialog").Should().NotBeNull();
        cut.Find(".dialog-title").TextContent.Should().Contain("Confirmar Suspensão Forçada");
        cut.Find(".dialog-message").TextContent.Should().Contain("Esta ação interromperá imediatamente o acesso da agência.");
        cut.Find("input.confirmation-text-input").Should().NotBeNull();
        cut.Find("textarea.reason-input").Should().NotBeNull();
    }

    /// <summary>
    /// Valida que o botão de confirmação inicia rigorosamente desabilitado.
    /// </summary>
    [Fact]
    public void ConfirmActionDialog_Initially_ConfirmButtonShouldBeDisabled()
    {
        // Act
        var cut = Render<ConfirmActionDialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Title, "Suspender Tenant")
            .Add(p => p.ExpectedConfirmationText, "alphatech")
            .Add(p => p.RequireReason, true));

        // Assert
        var confirmButton = cut.Find("button.btn-dialog-confirm");
        confirmButton.HasAttribute("disabled").Should().BeTrue();
    }

    /// <summary>
    /// Valida que preencher apenas a justificativa sem fornecer o texto exato de confirmação mantém o botão desabilitado.
    /// </summary>
    [Fact]
    public void ConfirmActionDialog_WhenOnlyReasonEntered_ConfirmButtonShouldRemainDisabled()
    {
        // Arrange
        var cut = Render<ConfirmActionDialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Title, "Suspender Tenant")
            .Add(p => p.ExpectedConfirmationText, "alphatech")
            .Add(p => p.RequireReason, true));

        // Act
        var reasonInput = cut.Find("textarea.reason-input");
        reasonInput.Input("Inadimplência recorrente após término do grace period.");

        // Assert
        var confirmButton = cut.Find("button.btn-dialog-confirm");
        confirmButton.HasAttribute("disabled").Should().BeTrue();
    }

    /// <summary>
    /// Valida que preencher apenas o texto de confirmação sem preencher a justificativa obrigatória mantém o botão desabilitado.
    /// </summary>
    [Fact]
    public void ConfirmActionDialog_WhenOnlyConfirmationTextEntered_ConfirmButtonShouldRemainDisabled()
    {
        // Arrange
        var cut = Render<ConfirmActionDialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Title, "Suspender Tenant")
            .Add(p => p.ExpectedConfirmationText, "alphatech")
            .Add(p => p.RequireReason, true));

        // Act
        var textInput = cut.Find("input.confirmation-text-input");
        textInput.Input("alphatech");

        // Assert
        var confirmButton = cut.Find("button.btn-dialog-confirm");
        confirmButton.HasAttribute("disabled").Should().BeTrue();
    }

    /// <summary>
    /// Valida que ao preencher a justificativa com tamanho mínimo (>= 5 caracteres) e o texto de confirmação exato,
    /// o botão de confirmação torna-se habilitado.
    /// </summary>
    [Fact]
    public void ConfirmActionDialog_WhenBothCriteriaSatisfied_ConfirmButtonShouldBeEnabled()
    {
        // Arrange
        var cut = Render<ConfirmActionDialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Title, "Suspender Tenant")
            .Add(p => p.ExpectedConfirmationText, "alphatech")
            .Add(p => p.RequireReason, true));

        // Act
        cut.Find("textarea.reason-input").Input("Inadimplência recorrente.");
        cut.Find("input.confirmation-text-input").Input("alphatech");

        // Assert
        var confirmButton = cut.Find("button.btn-dialog-confirm");
        confirmButton.HasAttribute("disabled").Should().BeFalse();
    }

    /// <summary>
    /// Valida que ao clicar no botão de confirmação habilitado, o callback OnConfirm é disparado com a justificativa digitada.
    /// </summary>
    [Fact]
    public void ConfirmActionDialog_WhenConfirmClicked_ShouldEmitOnConfirmWithReason()
    {
        // Arrange
        string? confirmedReason = null;

        var cut = Render<ConfirmActionDialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Title, "Suspender Tenant")
            .Add(p => p.ExpectedConfirmationText, "alphatech")
            .Add(p => p.RequireReason, true)
            .Add(p => p.OnConfirm, EventCallback.Factory.Create<string>(this, r => confirmedReason = r)));

        cut.Find("textarea.reason-input").Input("Suspensão judicial determinada pelo jurídico.");
        cut.Find("input.confirmation-text-input").Input("alphatech");

        // Act
        cut.Find("button.btn-dialog-confirm").Click();

        // Assert
        confirmedReason.Should().Be("Suspensão judicial determinada pelo jurídico.");
    }

    /// <summary>
    /// Valida que ao clicar no botão Cancelar, o callback OnCancel é emitido.
    /// </summary>
    [Fact]
    public void ConfirmActionDialog_WhenCancelClicked_ShouldEmitOnCancel()
    {
        // Arrange
        var wasCancelled = false;

        var cut = Render<ConfirmActionDialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Title, "Suspender Tenant")
            .Add(p => p.ExpectedConfirmationText, "alphatech")
            .Add(p => p.OnCancel, EventCallback.Factory.Create(this, () => wasCancelled = true)));

        // Act
        cut.Find("button.btn-dialog-cancel").Click();

        // Assert
        wasCancelled.Should().BeTrue();
    }

    /// <summary>
    /// Valida que durante o estado de processamento assíncrono (IsProcessing = true), os botões são desabilitados.
    /// </summary>
    [Fact]
    public void ConfirmActionDialog_WhenIsProcessingIsTrue_ShouldDisableButtonsAndShowSpinner()
    {
        // Arrange & Act
        var cut = Render<ConfirmActionDialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Title, "Suspender Tenant")
            .Add(p => p.ExpectedConfirmationText, "alphatech")
            .Add(p => p.IsProcessing, true));

        // Assert
        cut.Find(".spinner-inline").Should().NotBeNull();
        cut.Find("button.btn-dialog-confirm").HasAttribute("disabled").Should().BeTrue();
        cut.Find("button.btn-dialog-cancel").HasAttribute("disabled").Should().BeTrue();
    }
}
