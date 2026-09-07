namespace WebApi.Models;

/// <summary>
/// Carga útil para conexão de uma conta de anúncios em modo demonstração para aceleração do FTUX.
/// </summary>
/// <param name="WorkspaceId">Identificador do workspace que receberá a conta demonstrativa.</param>
/// <param name="Platform">Plataforma de anúncios (padrão: MetaAds).</param>
public sealed record ConnectDemoAccountApiRequest(
    Guid WorkspaceId,
    string Platform = "MetaAds");

/// <summary>
/// Carga útil para convidar/cadastrar um colaborador na agência.
/// </summary>
/// <param name="FullName">Nome completo do colaborador.</param>
/// <param name="Email">Endereço de e-mail corporativo.</param>
/// <param name="Role">Papel de governança (ex: MediaManager, Analyst).</param>
/// <param name="PhoneNumber">Telefone opcional de contato.</param>
public sealed record InviteTenantUserApiRequest(
    string FullName,
    string Email,
    string Role,
    string? PhoneNumber = null);
