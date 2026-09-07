namespace Master.Domain.Users;

/// <summary>
/// Constantes com os nomes dos perfis corporativos do catálogo Master para autorização e RBAC.
/// </summary>
public static class MasterRoles
{
    /// <summary>
    /// Administrador com acesso irrestrito a todos os recursos da plataforma.
    /// </summary>
    public const string SuperAdmin = "SuperAdmin";

    /// <summary>
    /// Técnico de suporte corporativo para diagnósticos operacionais e sessões de Shadow Mode.
    /// </summary>
    public const string SupportTechnician = "SupportTechnician";
}
