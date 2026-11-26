namespace SocialPublishing.Application.Brand.DTOs;

/// <summary>
/// Conteúdo da logo do cliente para prévia autenticada.
/// </summary>
/// <param name="ContentType">Tipo MIME da imagem.</param>
/// <param name="ContentBase64">Bytes codificados em Base64.</param>
public sealed record BrandVoiceLogoContentDto(string ContentType, string ContentBase64);