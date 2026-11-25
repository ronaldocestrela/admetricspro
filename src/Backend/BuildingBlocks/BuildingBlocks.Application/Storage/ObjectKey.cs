using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Application.Storage;

/// <summary>
/// Chave de objeto validada em relação a um tenant.
/// </summary>
/// <param name="Value">Texto da chave relativa no bucket (segmentos separados por <c>/</c>).</param>
public sealed record ObjectKey(string Value)
{
    /// <summary>
    /// Valida a chave bruta para o tenant informado.
    /// </summary>
    /// <param name="key">Chave relativa no bucket.</param>
    /// <param name="tenantId">Tenant dono do objeto.</param>
    /// <returns>Chave validada ou <see cref="StorageErrors.KeyInvalid"/>.</returns>
    public static Result<ObjectKey> Create(string? key, Guid tenantId)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Result<ObjectKey>.Failure(StorageErrors.KeyInvalid);
        }

        var trimmed = key.Trim();
        if (trimmed.Contains('\\', StringComparison.Ordinal) || trimmed.Contains("..", StringComparison.Ordinal))
        {
            return Result<ObjectKey>.Failure(StorageErrors.KeyInvalid);
        }

        var segments = trimmed.Split('/', StringSplitOptions.None);
        if (segments.Length == 0 || segments.Any(static s => string.IsNullOrEmpty(s)))
        {
            return Result<ObjectKey>.Failure(StorageErrors.KeyInvalid);
        }

        var tenantSegment = tenantId.ToString("N");

        if (segments.Length == 5
            && string.Equals(segments[0], "organic-media", StringComparison.Ordinal)
            && (string.Equals(segments[1], "staging", StringComparison.Ordinal)
                || string.Equals(segments[1], "durable", StringComparison.Ordinal)))
        {
            if (!string.Equals(segments[2], tenantSegment, StringComparison.OrdinalIgnoreCase))
            {
                return Result<ObjectKey>.Failure(StorageErrors.KeyInvalid);
            }

            if (!IsGuidNFormat(segments[3]) || !IsGuidNFormat(segments[4]))
            {
                return Result<ObjectKey>.Failure(StorageErrors.KeyInvalid);
            }

            return Result<ObjectKey>.Success(new ObjectKey(trimmed));
        }

        if (!string.Equals(segments[0], tenantSegment, StringComparison.OrdinalIgnoreCase))
        {
            return Result<ObjectKey>.Failure(StorageErrors.KeyInvalid);
        }

        if (segments.Length < 2)
        {
            return Result<ObjectKey>.Failure(StorageErrors.KeyInvalid);
        }

        return Result<ObjectKey>.Success(new ObjectKey(trimmed));
    }

    private static bool IsGuidNFormat(string segment) =>
        segment.Length == 32
        && Guid.TryParseExact(segment, "N", out _);
}