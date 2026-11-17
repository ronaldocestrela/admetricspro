using MailKit.Security;

namespace BuildingBlocks.Infrastructure.Emails;

/// <summary>
/// Contrato de baixo nível para entrega de mensagens MIME via SMTP (testável com implementações substituíveis).
/// </summary>
public interface ISmtpMailTransport
{
    /// <summary>
    /// Conecta ao servidor SMTP e envia a mensagem conforme o pedido validado.
    /// </summary>
    /// <param name="request">Parâmetros de conexão e mensagem MIME a enviar.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Tarefa que representa o envio concluído.</returns>
    Task SendAsync(SmtpMailSendRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Pedido validado de envio SMTP incluindo conexão e conteúdo MIME.
/// </summary>
/// <param name="Host">Host do servidor SMTP.</param>
/// <param name="Port">Porta do servidor SMTP.</param>
/// <param name="SecureSocket">Modo de segurança da conexão (STARTTLS, SSL, etc.).</param>
/// <param name="TimeoutSeconds">Timeout da operação em segundos.</param>
/// <param name="UserName">Usuário SMTP opcional para autenticação.</param>
/// <param name="Password">Senha SMTP opcional para autenticação.</param>
/// <param name="Message">Mensagem MIME pronta para envio.</param>
public sealed record SmtpMailSendRequest(
    string Host,
    int Port,
    SecureSocketOptions SecureSocket,
    int TimeoutSeconds,
    string? UserName,
    string? Password,
    MimeKit.MimeMessage Message);

/// <summary>
/// Exceção de entrega SMTP classificada como transitória ou permanente para política de retry.
/// </summary>
public sealed class SmtpDeliveryException : Exception
{
    /// <summary>
    /// Inicializa uma nova instância de <see cref="SmtpDeliveryException"/>.
    /// </summary>
    /// <param name="message">Descrição da falha.</param>
    /// <param name="isTransient">Indica se a falha é transitória e elegível a retry.</param>
    public SmtpDeliveryException(string message, bool isTransient)
        : base(message)
    {
        IsTransient = isTransient;
    }

    /// <summary>
    /// Indica se a falha é transitória (rede, timeout ou resposta SMTP 4xx).
    /// </summary>
    public bool IsTransient { get; }
}