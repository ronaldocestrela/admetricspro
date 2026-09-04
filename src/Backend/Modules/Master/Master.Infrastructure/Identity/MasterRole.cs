using Microsoft.AspNetCore.Identity;

namespace Master.Infrastructure.Identity;

/// <summary>
/// Papel corporativo atribuído aos operadores do Backoffice para controle de autorização baseado em perfil (RBAC).
/// </summary>
public sealed class MasterRole : IdentityRole<Guid>
{
    /// <summary>
    /// Papel com privilégios irrestritos de administração global de tenants, planos, infraestrutura e segurança.
    /// </summary>
    public const string SuperAdmin = "SuperAdmin";

    /// <summary>
    /// Papel para técnicos de suporte técnico, com foco em diagnóstico de integrações e shadow mode auditado.
    /// </summary>
    public const string SupportTechnician = "SupportTechnician";

    /// <summary>
    /// Inicializa uma nova instância de <see cref="MasterRole"/> com identificador único primário.
    /// </summary>
    public MasterRole()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Inicializa uma nova instância de <see cref="MasterRole"/> com nome normalizado e descrição funcional.
    /// </summary>
    /// <param name="roleName">Nome do perfil de acesso.</param>
    /// <param name="description">Descrição detalhada do escopo de permissões do perfil.</param>
    public MasterRole(string roleName, string description)
        : this()
    {
        Name = roleName;
        NormalizedName = roleName.ToUpperInvariant();
        Description = description;
    }

    /// <summary>
    /// Obtém ou define a descrição funcional do papel administrativo.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
