using BuildingBlocks.Application.Emails;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.Emails;
using FluentAssertions;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace UnitTests.Backend.Emails;

/// <summary>
/// Testes unitários do <see cref="SmtpEmailSender"/> com transporte SMTP substituível.
/// </summary>
public sealed class SmtpEmailSenderTests
{
    private readonly ISmtpMailTransport _transport = Substitute.For<ISmtpMailTransport>();
    private readonly ILogger<SmtpEmailSender> _logger = Substitute.For<ILogger<SmtpEmailSender>>();

    /// <summary>
    /// Configuração SMTP válida padrão para os cenários de sucesso.
    /// </summary>
    private static EmailOptions ValidOptions() => new()
    {
        Provider = "Smtp",
        SmtpHost = "smtp.example.com",
        SmtpPort = 587,
        EnableSsl = true,
        SecureSocket = EmailSecureSocketMode.StartTls,
        FromEmail = "noreply@admetricspro.com.br",
        FromDisplayName = "AdMetricsPro",
        UserName = "smtp-user",
        Password = "smtp-secret",
        TimeoutSeconds = 10,
        MaxRetryAttempts = 1
    };

    private SmtpEmailSender CreateSender(EmailOptions options) =>
        new(Options.Create(options), _transport, _logger);

    /// <summary>
    /// Host SMTP vazio deve falhar na validação sem invocar o transporte.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendEmailAsync_WhenSmtpHostMissing_ShouldReturnInvalidConfigurationWithoutCallingTransport(string host)
    {
        var options = ValidOptions();
        options.SmtpHost = host;
        var sender = CreateSender(options);
        var message = EmailMessage.Create("dest@example.com", "Assunto", "<p>HTML</p>", "Texto").Value;

        var result = await sender.SendEmailAsync(message, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Email.InvalidConfiguration");
        await _transport.DidNotReceive().SendAsync(Arg.Any<SmtpMailSendRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Porta SMTP inválida deve falhar na validação sem invocar o transporte.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(70000)]
    public async Task SendEmailAsync_WhenSmtpPortInvalid_ShouldReturnInvalidConfiguration(int port)
    {
        var options = ValidOptions();
        options.SmtpPort = port;
        var sender = CreateSender(options);
        var message = EmailMessage.Create("dest@example.com", "Assunto", "<p>HTML</p>", "Texto").Value;

        var result = await sender.SendEmailAsync(message, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Email.InvalidConfiguration");
        await _transport.DidNotReceive().SendAsync(Arg.Any<SmtpMailSendRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Remetente padrão ausente deve falhar na validação sem invocar o transporte.
    /// </summary>
    [Fact]
    public async Task SendEmailAsync_WhenFromEmailMissing_ShouldReturnInvalidConfiguration()
    {
        var options = ValidOptions();
        options.FromEmail = "";
        var sender = CreateSender(options);
        var message = EmailMessage.Create("dest@example.com", "Assunto", "<p>HTML</p>", "Texto").Value;

        var result = await sender.SendEmailAsync(message, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Email.InvalidConfiguration");
        await _transport.DidNotReceive().SendAsync(Arg.Any<SmtpMailSendRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Credencial parcialmente preenchida deve falhar na validação.
    /// </summary>
    [Fact]
    public async Task SendEmailAsync_WhenOnlyUsernameProvided_ShouldReturnInvalidConfiguration()
    {
        var options = ValidOptions();
        options.Password = "";
        var sender = CreateSender(options);
        var message = EmailMessage.Create("dest@example.com", "Assunto", "<p>HTML</p>", "Texto").Value;

        var result = await sender.SendEmailAsync(message, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Email.InvalidConfiguration");
        await _transport.DidNotReceive().SendAsync(Arg.Any<SmtpMailSendRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Envio bem-sucedido deve repassar host, TLS, timeout e mensagem MIME ao transporte.
    /// </summary>
    [Fact]
    public async Task SendEmailAsync_WhenConfigurationValid_ShouldDispatchThroughTransport()
    {
        var options = ValidOptions();
        var sender = CreateSender(options);
        var message = EmailMessage.Create(
            "dest@example.com",
            "Assunto de Teste",
            "<p>Corpo HTML</p>",
            "Corpo texto").Value;

        SmtpMailSendRequest? captured = null;
        await _transport.SendAsync(Arg.Do<SmtpMailSendRequest>(r => captured = r), Arg.Any<CancellationToken>());

        var result = await sender.SendEmailAsync(message, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.Host.Should().Be("smtp.example.com");
        captured.Port.Should().Be(587);
        captured.SecureSocket.Should().Be(SecureSocketOptions.StartTls);
        captured.TimeoutSeconds.Should().Be(10);
        captured.UserName.Should().Be("smtp-user");
        captured.Password.Should().Be("smtp-secret");
        captured.Message.To.Mailboxes.Single().Address.Should().Be("dest@example.com");
        captured.Message.Subject.Should().Be("Assunto de Teste");
        captured.Message.From.Mailboxes.Single().Address.Should().Be("noreply@admetricspro.com.br");
        captured.Message.From.Mailboxes.Single().Name.Should().Be("AdMetricsPro");
    }

    /// <summary>
    /// Remetente customizado na mensagem deve substituir o remetente padrão da plataforma.
    /// </summary>
    [Fact]
    public async Task SendEmailAsync_WhenMessageSpecifiesFrom_ShouldOverridePlatformSender()
    {
        var options = ValidOptions();
        var sender = CreateSender(options);
        var message = EmailMessage.Create(
            "dest@example.com",
            "Assunto",
            "<p>HTML</p>",
            "Texto",
            fromEmail: "custom@agency.com",
            fromDisplayName: "Agência Custom").Value;

        SmtpMailSendRequest? captured = null;
        await _transport.SendAsync(Arg.Do<SmtpMailSendRequest>(r => captured = r), Arg.Any<CancellationToken>());

        var result = await sender.SendEmailAsync(message, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        captured!.Message.From.Mailboxes.Single().Address.Should().Be("custom@agency.com");
        captured.Message.From.Mailboxes.Single().Name.Should().Be("Agência Custom");
    }

    /// <summary>
    /// Falha transitória seguida de sucesso deve realizar exatamente duas tentativas no transporte.
    /// </summary>
    [Fact]
    public async Task SendEmailAsync_WhenTransientFailureThenSuccess_ShouldRetryOnce()
    {
        var options = ValidOptions();
        var sender = CreateSender(options);
        var message = EmailMessage.Create("dest@example.com", "Assunto", "<p>HTML</p>", "Texto").Value;

        _transport
            .SendAsync(Arg.Any<SmtpMailSendRequest>(), Arg.Any<CancellationToken>())
            .Returns(
                _ => throw new SmtpDeliveryException("Temporário", isTransient: true),
                _ => Task.CompletedTask);

        var result = await sender.SendEmailAsync(message, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _transport.Received(2).SendAsync(Arg.Any<SmtpMailSendRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Falha de autenticação não deve ser reenviada.
    /// </summary>
    [Fact]
    public async Task SendEmailAsync_WhenAuthenticationFails_ShouldNotRetry()
    {
        var options = ValidOptions();
        var sender = CreateSender(options);
        var message = EmailMessage.Create("dest@example.com", "Assunto", "<p>HTML</p>", "Texto").Value;

        _transport
            .SendAsync(Arg.Any<SmtpMailSendRequest>(), Arg.Any<CancellationToken>())
            .Throws(new SmtpDeliveryException("Credenciais inválidas", isTransient: false));

        var result = await sender.SendEmailAsync(message, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Email.SendFailed");
        await _transport.Received(1).SendAsync(Arg.Any<SmtpMailSendRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Duas falhas transitórias consecutivas devem resultar em Email.SendFailed.
    /// </summary>
    [Fact]
    public async Task SendEmailAsync_WhenTransientFailuresExhaustRetries_ShouldReturnSendFailed()
    {
        var options = ValidOptions();
        var sender = CreateSender(options);
        var message = EmailMessage.Create("dest@example.com", "Assunto", "<p>HTML</p>", "Texto").Value;

        _transport
            .SendAsync(Arg.Any<SmtpMailSendRequest>(), Arg.Any<CancellationToken>())
            .Throws(new SmtpDeliveryException("Indisponível", isTransient: true));

        var result = await sender.SendEmailAsync(message, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Email.SendFailed");
        await _transport.Received(2).SendAsync(Arg.Any<SmtpMailSendRequest>(), Arg.Any<CancellationToken>());
    }
}