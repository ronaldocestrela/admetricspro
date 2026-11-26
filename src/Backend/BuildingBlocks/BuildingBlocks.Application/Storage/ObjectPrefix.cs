using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Application.Storage;

/// <summary>
/// Validação de prefixos de exclusão em massa por tenant.
/// </summary>
public static class ObjectPrefix
{
    /// <summary>
    /// Garante que o prefixo referencia apenas o tenant informado.
    /// </summary>
    /// <param name="prefix">Prefixo relativo no bucket.</param>
    /// <param name="tenantId">Tenant dono esperado.</param>
    /// <returns>Sucesso ou <see cref="StorageErrors.KeyInvalid"/>.</returns>
    public static Result ValidateForTenant(string? prefix, Guid tenantId)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return Result.Failure(StorageErrors.KeyInvalid);
        }

        var trimmed = prefix.Trim().TrimEnd('/');
        if (trimmed.Contains('\\', StringComparison.Ordinal) || trimmed.Contains("..", StringComparison.Ordinal))
        {
            return Result.Failure(StorageErrors.KeyInvalid);
        }

        var tenantSegment = tenantId.ToString("N");
        var segments = trimmed.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0 || segments.Any(static s => string.IsNullOrEmpty(s)))
        {
            return Result.Failure(StorageErrors.KeyInvalid);
        }

        var hasTenantSegment = segments.Any(s => string.Equals(s, tenantSegment, StringComparison.OrdinalIgnoreCase));
        if (!hasTenantSegment)
        {
            return Result.Failure(StorageErrors.KeyInvalid);
        }

        if (string.Equals(segments[0], "organic-media", StringComparison.Ordinal))
        {
            if (segments.Length < 3 || !string.Equals(segments[2], tenantSegment, StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure(StorageErrors.KeyInvalid);
            }

            return Result.Success();
        }

        if (!string.Equals(segments[0], tenantSegment, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure(StorageErrors.KeyInvalid);
        }

        return Result.Success();
    }
}