using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SocialPublishing.Infrastructure.Persistence;

/// <summary>
/// Conversor EF que marca instantes <c>datetime2</c> relidos do SQL Server como <see cref="DateTimeKind.Utc"/>.
/// </summary>
public sealed class UtcDateTimeValueConverter : ValueConverter<DateTime, DateTime>
{
    /// <summary>
    /// Inicializa o conversor com escrita inalterada e leitura com kind UTC.
    /// </summary>
    public UtcDateTimeValueConverter()
        : base(
            static v => DateTime.SpecifyKind(v, DateTimeKind.Unspecified),
            static v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }
}

/// <summary>
/// Variante anulável de <see cref="UtcDateTimeValueConverter"/>.
/// </summary>
public sealed class NullableUtcDateTimeValueConverter : ValueConverter<DateTime?, DateTime?>
{
    /// <summary>
    /// Inicializa o conversor com escrita inalterada e leitura com kind UTC quando presente.
    /// </summary>
    public NullableUtcDateTimeValueConverter()
        : base(
            static v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Unspecified) : v,
            static v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v)
    {
    }
}