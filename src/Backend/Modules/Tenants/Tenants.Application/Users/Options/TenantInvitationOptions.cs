namespace Tenants.Application.Users.Options;

/// <summary>
/// Opções de configuração para convites de colaboradores.
/// </summary>
public sealed class TenantInvitationOptions
{
    /// <summary>
    /// Nome da seção em appsettings.
    /// </summary>
    public const string SectionName = "Tenants:Invitations";

    /// <summary>
    /// URL base pública do WebApp usada para montar links de aceite (ex.: https://app.admetricspro.com).
    /// </summary>
    public string WebAppBaseUrl { get; set; } = "http://localhost:5001";

    /// <summary>
    /// Validade do convite em horas.
    /// </summary>
    public int TokenValidityHours { get; set; } = 72;
}