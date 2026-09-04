namespace Master.Infrastructure.Configuration;

/// <summary>
/// Opções de configuração para o seed inicial do Super Administrador do Backoffice obtidas via variáveis de ambiente (.env).
/// </summary>
public sealed class SuperAdminSeedOptions
{
    /// <summary>
    /// Nome da seção no arquivo de configuração / variáveis de ambiente.
    /// </summary>
    public const string SectionName = "SuperAdmin";

    /// <summary>
    /// E-mail corporativo do Super Administrador inicial.
    /// </summary>
    public string Email { get; set; } = "admin@admetricspro.internal";

    /// <summary>
    /// Senha de acesso do Super Administrador inicial.
    /// </summary>
    public string Password { get; set; } = "SuperAdmin@Secure2026!";

    /// <summary>
    /// Nome completo do Super Administrador inicial.
    /// </summary>
    public string FullName { get; set; } = "Administrador Global AdMetricsPro";

    /// <summary>
    /// Papel de autorização inicial atribuído ao usuário.
    /// </summary>
    public string Role { get; set; } = "SuperAdmin";
}
