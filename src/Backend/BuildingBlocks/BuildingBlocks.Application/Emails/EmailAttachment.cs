namespace BuildingBlocks.Application.Emails;

/// <summary>
/// Anexo opcional de e-mail transacional.
/// </summary>
/// <param name="FileName">Nome do arquivo exibido ao destinatário.</param>
/// <param name="ContentType">Tipo MIME do anexo.</param>
/// <param name="Content">Conteúdo binário.</param>
public sealed record EmailAttachment(string FileName, string ContentType, byte[] Content);