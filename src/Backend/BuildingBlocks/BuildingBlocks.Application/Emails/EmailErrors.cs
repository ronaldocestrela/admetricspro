using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Application.Emails;

/// <summary>
/// Coleção de erros padronizados para validações e operações de envio de e-mails transacionais.
/// </summary>
public static class EmailErrors
{
    /// <summary>
    /// Erro de endereço de e-mail de destinatário inválido ou vazio.
    /// </summary>
    public static readonly Error InvalidRecipient = Error.Validation(
        "Email.InvalidRecipient",
        "O endereço de e-mail do destinatário é obrigatório e deve possuir um formato válido.");

    /// <summary>
    /// Erro de assunto de e-mail ausente.
    /// </summary>
    public static readonly Error SubjectRequired = Error.Validation(
        "Email.SubjectRequired",
        "O assunto do e-mail é obrigatório.");

    /// <summary>
    /// Erro de corpo de e-mail ausente.
    /// </summary>
    public static readonly Error BodyRequired = Error.Validation(
        "Email.BodyRequired",
        "O corpo do e-mail (HTML ou texto plano) é obrigatório.");

    /// <summary>
    /// Erro emitido quando o provedor de mensageria falha ao despachar a mensagem.
    /// </summary>
    /// <param name="details">Detalhes técnicos ou mensagem de erro da falha.</param>
    /// <returns>Instância de <see cref="Error"/> do tipo Failure.</returns>
    public static Error SendFailed(string details) => Error.Failure(
        "Email.SendFailed",
        $"Falha ao enviar e-mail transacional: {details}");
}
