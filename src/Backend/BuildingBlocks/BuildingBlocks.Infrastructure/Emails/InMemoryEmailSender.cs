using System.Collections.Concurrent;
using BuildingBlocks.Application.Emails;
using BuildingBlocks.Domain.Primitives;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.Emails;

/// <summary>
/// Provedor in-memory de envio de e-mails transacionais para testes unitários, testes de aceitação e desenvolvimento local.
/// </summary>
public sealed class InMemoryEmailSender : IEmailSender
{
    private readonly ConcurrentBag<EmailMessage> _sentMessages = new();
    private readonly ILogger<InMemoryEmailSender>? _logger;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="InMemoryEmailSender"/>.
    /// </summary>
    /// <param name="logger">Logger estruturado opcional.</param>
    public InMemoryEmailSender(ILogger<InMemoryEmailSender>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Obtém a lista somente-leitura de todas as mensagens de e-mail enviadas pela instância.
    /// </summary>
    public IReadOnlyList<EmailMessage> SentMessages => _sentMessages.ToArray();

    /// <inheritdoc />
    public Task<Result> SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (message is null)
        {
            return Task.FromResult(Result.Failure(EmailErrors.InvalidRecipient));
        }

        _sentMessages.Add(message);

        _logger?.LogInformation(
            "E-mail transacional (InMemory) enfileirado para {To} com assunto \"{Subject}\"",
            message.To,
            message.Subject);

        return Task.FromResult(Result.Success());
    }

    /// <summary>
    /// Limpa o histórico de mensagens enviadas.
    /// </summary>
    public void Clear()
    {
        _sentMessages.Clear();
    }
}
