namespace BuildingBlocks.Domain.Tenants;

/// <summary>
/// Representa o estado operacional de uma conta de colaborador no inquilino.
/// </summary>
public enum TenantUserAccountStatus
{
    /// <summary>
    /// Conta ativa com credenciais válidas e acesso liberado.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Convite emitido aguardando o colaborador definir a senha de acesso.
    /// </summary>
    PendingAcceptance = 2,

    /// <summary>
    /// Conta desativada ou inativa sem convite pendente.
    /// </summary>
    Inactive = 3
}