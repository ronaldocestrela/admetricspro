using BuildingBlocks.Domain.Primitives;

namespace Tenants.Application.Branding;

/// <summary>
/// Valida cabeçalhos de imagens para upload de branding (PNG, JPEG, WEBP, ICO, SVG).
/// </summary>
public static class TenantBrandAssetHeaderInspector
{
    /// <summary>
    /// Confere se o conteúdo corresponde ao MIME declarado.
    /// </summary>
    /// <param name="content">Fluxo de leitura (reposicionável).</param>
    /// <param name="contentType">MIME declarado.</param>
    /// <param name="fileName">Nome original do arquivo para extensão.</param>
    /// <param name="slot">Slot do ativo (regras de extensão).</param>
    /// <returns>Sucesso ou erro de validação.</returns>
    public static Result Validate(Stream content, string contentType, string? fileName, TenantBrandAssetSlot slot)
    {
        if (content is null || content.Length < 1)
        {
            return Result.Failure(Error.Validation(
                "TenantBranding.AssetEmpty",
                "O arquivo de marca não pode estar vazio."));
        }

        if (content.Length > TenantBrandingAssetLimits.MaxUploadBytes)
        {
            return Result.Failure(Error.Validation(
                "TenantBranding.AssetTooLarge",
                "O arquivo de marca não pode exceder 2 MB."));
        }

        var extension = Path.GetExtension(fileName ?? string.Empty);
        var extResult = ValidateExtension(extension, slot);
        if (extResult.IsFailure)
        {
            return extResult;
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
            "image/x-icon" or "image/vnd.microsoft.icon" => ValidateIco(content),
            "image/svg+xml" => ValidateSvg(content),
            _ => Result.Failure(Error.Validation(
                "TenantBranding.InvalidAssetMime",
                "Tipo de arquivo de marca não permitido.")),
        };
    }

    private static Result ValidateExtension(string? extension, TenantBrandAssetSlot slot)
    {
        var allowed = slot == TenantBrandAssetSlot.Favicon
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".ico", ".png", ".svg" }
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png", ".svg", ".jpg", ".jpeg", ".webp" };

        if (string.IsNullOrWhiteSpace(extension) || !allowed.Contains(extension))
        {
            return Result.Failure(Error.Validation(
                slot == TenantBrandAssetSlot.Favicon
                    ? "TenantBranding.InvalidFaviconFormat"
                    : "TenantBranding.InvalidLightLogoFormat",
                $"Formato de arquivo inválido. Permitidos: {string.Join(", ", allowed)}."));
        }

        return Result.Success();
    }

    private static Result ValidatePng(Stream content)
    {
        Span<byte> header = stackalloc byte[8];
        if (content.Read(header) < 8)
        {
            return Result.Failure(Error.Validation("TenantBranding.AssetUnreadable", "Arquivo PNG inválido ou truncado."));
        }

        if (header[0] != 0x89 || header[1] != (byte)'P' || header[2] != (byte)'N' || header[3] != (byte)'G')
        {
            return Result.Failure(Error.Validation("TenantBranding.AssetUnreadable", "Cabeçalho PNG não reconhecido."));
        }

        return Result.Success();
    }

    private static Result ValidateJpeg(Stream content)
    {
        if (content.ReadByte() != 0xFF || content.ReadByte() != 0xD8)
        {
            return Result.Failure(Error.Validation("TenantBranding.AssetUnreadable", "Cabeçalho JPEG não reconhecido."));
        }

        return Result.Success();
    }

    private static Result ValidateWebp(Stream content)
    {
        Span<byte> header = stackalloc byte[12];
        if (content.Read(header) < 12)
        {
            return Result.Failure(Error.Validation("TenantBranding.AssetUnreadable", "Arquivo WEBP inválido ou truncado."));
        }

        if (header[0] != (byte)'R' || header[1] != (byte)'I' || header[2] != (byte)'F' || header[3] != (byte)'F'
            || header[8] != (byte)'W' || header[9] != (byte)'E' || header[10] != (byte)'B' || header[11] != (byte)'P')
        {
            return Result.Failure(Error.Validation("TenantBranding.AssetUnreadable", "Cabeçalho WEBP não reconhecido."));
        }

        return Result.Success();
    }

    private static Result ValidateIco(Stream content)
    {
        Span<byte> header = stackalloc byte[4];
        if (content.Read(header) < 4)
        {
            return Result.Failure(Error.Validation("TenantBranding.AssetUnreadable", "Arquivo ICO inválido ou truncado."));
        }

        if (header[0] != 0 || header[1] != 0 || header[2] != 1 || header[3] != 0)
        {
            return Result.Failure(Error.Validation("TenantBranding.AssetUnreadable", "Cabeçalho ICO não reconhecido."));
        }

        return Result.Success();
    }

    private static Result ValidateSvg(Stream content)
    {
        using var reader = new StreamReader(content, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var text = reader.ReadToEnd();
        if (text.Length > TenantBrandingAssetLimits.MaxSvgInspectChars)
        {
            return Result.Failure(Error.Validation("TenantBranding.AssetUnreadable", "SVG excede o tamanho permitido para inspeção."));
        }

        var lower = text.ToLowerInvariant();
        if (lower.Contains("<script", StringComparison.Ordinal)
            || lower.Contains("javascript:", StringComparison.Ordinal))
        {
            return Result.Failure(Error.Validation("TenantBranding.AssetUnreadable", "SVG contém conteúdo não permitido."));
        }

        if (!lower.Contains("<svg", StringComparison.Ordinal))
        {
            return Result.Failure(Error.Validation("TenantBranding.AssetUnreadable", "Documento SVG inválido."));
        }

        return Result.Success();
    }
}