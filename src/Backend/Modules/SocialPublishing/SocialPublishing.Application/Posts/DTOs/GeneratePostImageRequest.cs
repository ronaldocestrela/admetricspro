namespace SocialPublishing.Application.Posts.DTOs;

/// <summary>
/// Corpo HTTP opcional para geração de imagem de post aprovado.
/// </summary>
/// <param name="Direction">Direcionamento visual adicional da agência (até 500 caracteres).</param>
public sealed record GeneratePostImageRequest(string? Direction = null);