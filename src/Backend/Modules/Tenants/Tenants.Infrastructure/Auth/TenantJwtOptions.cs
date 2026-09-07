namespace Tenants.Infrastructure.Auth;

/// <summary>
/// Opções de configuração para emissão e validação de tokens JWT específicos dos inquilinos (tenants operacionais).
/// </summary>
public sealed class TenantJwtOptions
{
    /// <summary>
    /// Nome padrão da seção de configuração no appsettings.json ou variáveis de ambiente.
    /// </summary>
    public const string SectionName = "TenantJwt";

    /// <summary>
    /// Chave simétrica secreta de assinatura criptográfica HMAC-SHA256 (deve ter no mínimo 32 caracteres / 256 bits).
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Emissor confiável do token (Issuer).
    /// </summary>
    public string Issuer { get; set; } = "AdMetricsPro.Tenants";

    /// <summary>
    /// Audiência esperada para o token (Audience).
    /// </summary>
    public string Audience { get; set; } = "AdMetricsPro.ClientApp";

    /// <summary>
    /// Tempo de expiração do token em minutos (padrão: 480 min / 8 horas).
    /// </summary>
    public int ExpirationMinutes { get; set; } = 480;
}
