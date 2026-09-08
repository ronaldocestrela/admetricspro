using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Application.Emails;

/// <summary>
/// Contrato de serviço para envio de e-mails transacionais.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Despacha uma mensagem de e-mail transacional de forma assíncrona.
    /// </summary>
    /// <param name="message">Mensagem validada contendo destinatário, assunto e corpo.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Resultado da operação indicando sucesso ou falha técnica com código de erro tipado.</returns>
    Task<Result> SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
