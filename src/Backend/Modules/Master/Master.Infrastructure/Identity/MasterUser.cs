using Microsoft.AspNetCore.Identity;

namespace Master.Infrastructure.Identity;

/// <summary>
/// Entidade de identidade para usuários e operadores corporativos do console Backoffice da plataforma.
/// </summary>
public sealed class MasterUser : IdentityUser<Guid>
{
    /// <summary>
    /// Inicializa uma nova instância de <see cref="MasterUser"/> com chave única primária.
    /// </summary>
    public MasterUser()
    {
        Id = Guid.NewGuid();
        SecurityStamp = Guid.NewGuid().ToString("D");
        CreatedAtUtc = DateTime.UtcNow;
        IsActive = true;
    }

    /// <summary>
    /// Inicializa uma nova instância de <see cref="MasterUser"/> com dados cadastrais e de acesso.
    /// </summary>
    /// <param name="email">Endereço de e-mail corporativo.</param>
    /// <param name="fullName">Nome completo do operador.</param>
    public MasterUser(string email, string fullName)
        : this()
    {
        Email = email;
        UserName = email;
        FullName = fullName;
    }

    /// <summary>
    /// Obtém ou define o nome completo do operador corporativo.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define se a conta do operador está ativa no sistema.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Obtém ou define o timestamp UTC em que o registro do usuário foi criado.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Obtém ou define o timestamp UTC do último login efetuado com sucesso.
    /// </summary>
    public DateTime? LastLoginAtUtc { get; set; }

    /// <summary>
    /// Registra a ocorrência de autenticação com sucesso atualizando o timestamp de acesso.
    /// </summary>
    public void RecordLoginSuccess()
    {
        LastLoginAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Desativa a conta do operador impedindo acessos futuros ao Backoffice.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Reativa a conta do operador restaurando seu acesso ao Backoffice.
    /// </summary>
    public void Reactivate()
    {
        IsActive = true;
    }
}
