using System.Buffers.Binary;
using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Domain;

namespace SocialPublishing.Application.Brand;

/// <summary>
/// Valida cabeçalhos da logo do cliente (PNG, JPEG, WebP).
/// </summary>
public static class BrandLogoHeaderInspector
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp",
    };

    /// <summary>
    /// Confere tamanho, extensão e assinatura do arquivo.
    /// </summary>
    /// <param name="content">Fluxo de leitura.</param>
    /// <param name="contentType">MIME declarado.</param>
    /// <param name="fileName">Nome original do arquivo.</param>
    /// <returns>Sucesso ou erro de validação.</returns>
    public static Result Validate(Stream content, string contentType, string? fileName)
    {
        if (content is null || content.Length < 1)
        {
            return Result.Failure(SocialErrors.BrandLogoInvalid);
        }

        if (content.Length > BrandLogoAssetLimits.MaxUploadBytes)
        {
            return Result.Failure(SocialErrors.BrandLogoTooLarge);
        }

        var extension = Path.GetExtension(fileName ?? string.Empty);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            return Result.Failure(SocialErrors.BrandLogoInvalid);
        }

        if (content.CanSeek)
        {
            content.Position = 0;
        }

        var mime = contentType.Trim().ToLowerInvariant();
        return mime switch
        {
            "image/png" => ValidatePng(content),
            "image/jpeg" or "image/jpg" => ValidateJpeg(content),
            "image/webp" => ValidateWebp(content),
            _ => Result.Failure(SocialErrors.BrandLogoInvalid),
        };
    }

    private static Result ValidatePng(Stream content)
    {
        Span<byte> header = stackalloc byte[8];
        if (content.Read(header) < 8)
        {
            return Result.Failure(SocialErrors.BrandLogoInvalid);
        }

        if (header[0] != 0x89 || header[1] != (byte)'P' || header[2] != (byte)'N' || header[3] != (byte)'G')
        {
            return Result.Failure(SocialErrors.BrandLogoInvalid);
        }

        return Result.Success();
    }

    private static Result ValidateJpeg(Stream content)
    {
        if (content.ReadByte() != 0xFF || content.ReadByte() != 0xD8)
        {
            return Result.Failure(SocialErrors.BrandLogoInvalid);
        }

        return Result.Success();
    }

    private static Result ValidateWebp(Stream content)
    {
        Span<byte> header = stackalloc byte[12];
        if (content.Read(header) < 12)
        {
            return Result.Failure(SocialErrors.BrandLogoInvalid);
        }

        if (header[0] != (byte)'R' || header[1] != (byte)'I' || header[2] != (byte)'F' || header[3] != (byte)'F')
        {
            return Result.Failure(SocialErrors.BrandLogoInvalid);
        }

        if (header[8] != (byte)'W' || header[9] != (byte)'E' || header[10] != (byte)'B' || header[11] != (byte)'P')
        {
            return Result.Failure(SocialErrors.BrandLogoInvalid);
        }

        return Result.Success();
    }
}