namespace SocialPublishing.Application.Publishing;

/// <summary>
/// Identificador externo retornado pela rede após publicação bem-sucedida.
/// </summary>
/// <param name="ExternalId">Id estável do post ou publicação na plataforma.</param>
public sealed record OrganicPublishReceipt(string ExternalId);
