namespace SocialPublishing.Application.Posts.DTOs;

/// <summary>
/// Conteúdo binário de uma mídia de post, em Base64, para prévia autenticada.
/// </summary>
/// <param name="MimeType">Tipo MIME do arquivo.</param>
/// <param name="ContentBase64">Bytes do arquivo em Base64.</param>
public sealed record OrganicPostMediaContentDto(string MimeType, string ContentBase64);
