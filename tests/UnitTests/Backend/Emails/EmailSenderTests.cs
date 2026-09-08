using BuildingBlocks.Application.Emails;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.Emails;
using FluentAssertions;
using Xunit;

namespace UnitTests.Backend.Emails;

/// <summary>
/// Testes unitários para abstrações de envio de e-mails transacionais e sender in-memory.
/// </summary>
public sealed class EmailSenderTests
{
    /// <summary>
    /// Valida que a criação de uma mensagem de e-mail com campos válidos tem sucesso.
    /// </summary>
    [Fact]
    public void EmailMessage_WhenValidInputsProvided_ShouldCreateMessageSuccessfully()
    {
        // Act
        var result = EmailMessage.Create(
            to: "gestor@agencia.com.br",
            subject: "Bem-vindo ao AdMetricsPro",
            htmlBody: "<h1>Bem-vindo!</h1>",
            plainTextBody: "Bem-vindo!",
            fromEmail: "noreply@admetricspro.com.br",
            fromDisplayName: "AdMetricsPro Notificações");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.To.Should().Be("gestor@agencia.com.br");
        result.Value.Subject.Should().Be("Bem-vindo ao AdMetricsPro");
        result.Value.HtmlBody.Should().Be("<h1>Bem-vindo!</h1>");
        result.Value.PlainTextBody.Should().Be("Bem-vindo!");
        result.Value.FromEmail.Should().Be("noreply@admetricspro.com.br");
        result.Value.FromDisplayName.Should().Be("AdMetricsPro Notificações");
    }

    /// <summary>
    /// Valida que a tentativa de criar mensagem com e-mail inválido ou campos vazios falha com erro de validação.
    /// </summary>
    [Theory]
    [InlineData("", "Assunto", "Corpo")]
    [InlineData("   ", "Assunto", "Corpo")]
    [InlineData("email-invalido", "Assunto", "Corpo")]
    [InlineData("gestor@agencia.com.br", "", "Corpo")]
    [InlineData("gestor@agencia.com.br", "Assunto", "")]
    public void EmailMessage_WhenInvalidInputs_ShouldReturnFailure(string to, string subject, string body)
    {
        // Act
        var result = EmailMessage.Create(to, subject, body, body);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    /// <summary>
    /// Valida que o InMemoryEmailSender enfileira mensagens e permite inspeção de envios.
    /// </summary>
    [Fact]
    public async Task InMemoryEmailSender_WhenSendingValidMessage_ShouldStoreInSentMessages()
    {
        // Arrange
        var sender = new InMemoryEmailSender();
        var messageResult = EmailMessage.Create(
            "gestor@agencia.com.br",
            "Assunto de Teste",
            "<p>Conteúdo HTML</p>",
            "Conteúdo Texto");

        messageResult.IsSuccess.Should().BeTrue();

        // Act
        var sendResult = await sender.SendEmailAsync(messageResult.Value, CancellationToken.None);

        // Assert
        sendResult.IsSuccess.Should().BeTrue();
        sender.SentMessages.Should().HaveCount(1);
        sender.SentMessages[0].To.Should().Be("gestor@agencia.com.br");
        sender.SentMessages[0].Subject.Should().Be("Assunto de Teste");
    }

    /// <summary>
    /// Valida que o método Clear do InMemoryEmailSender esvazia a fila de mensagens enviadas.
    /// </summary>
    [Fact]
    public async Task InMemoryEmailSender_WhenClearCalled_ShouldEmptyCollection()
    {
        // Arrange
        var sender = new InMemoryEmailSender();
        var message = EmailMessage.Create("gestor@agencia.com.br", "Assunto", "<p>Corpo</p>", "Corpo").Value;
        await sender.SendEmailAsync(message, CancellationToken.None);

        // Act
        sender.Clear();

        // Assert
        sender.SentMessages.Should().BeEmpty();
    }
}
