namespace WebApp.Models;

/// <summary>
/// Modelo de apresentação detalhado para a ficha 360º de um inquilino no Backoffice Super Admin.
/// </summary>
/// <param name="Id">Identificador único do inquilino.</param>
/// <param name="CompanyName">Razão social ou denominação comercial da empresa.</param>
/// <param name="Cnpj">Cadastro Nacional da Pessoa Jurídica (14 dígitos numéricos).</param>
/// <param name="Subdomain">Subdomínio de roteamento do inquilino.</param>
/// <param name="CustomDomain">Domínio personalizado CNAME vinculado para White-Label, se configurado.</param>
/// <param name="Status">Status operacional no ciclo de vida (ex.: Active, Trial, Delinquent, Suspended, Cancelled).</param>
/// <param name="Tier">Nível do plano de assinatura (ex.: Trial, Starter, Pro, Enterprise).</param>
/// <param name="SubscriptionExpiresAtUtc">Data e hora em UTC de expiração da assinatura ou período de testes.</param>
/// <param name="CreatedAtUtc">Data e hora em UTC de provisionamento inicial do tenant.</param>
/// <param name="WorkspacesCount">Quantidade de workspaces configurados neste inquilino.</param>
/// <param name="SunkAdSpend">Volume total consolidado de verba publicitária gerenciada (em BRL).</param>
/// <param name="ActiveIntegrationsCount">Número de integrações de mídia ativas (Meta, Google, Bing, TikTok).</param>
/// <param name="TotalCampaignsCount">Número de campanhas sincronizadas em monitoramento contínuo.</param>
public sealed record Tenant360DetailsViewModel(
    Guid Id,
    string CompanyName,
    string Cnpj,
    string Subdomain,
    string? CustomDomain,
    string Status,
    string Tier,
    DateTime? SubscriptionExpiresAtUtc,
    DateTime CreatedAtUtc,
    int WorkspacesCount,
    decimal SunkAdSpend,
    int ActiveIntegrationsCount = 0,
    int TotalCampaignsCount = 0)
{
    /// <summary>
    /// Formata o CNPJ de 14 dígitos no padrão nacional (XX.XXX.XXX/XXXX-XX).
    /// </summary>
    public string FormattedCnpj => TenantDirectoryItemViewModel.FormatCnpj(Cnpj);

    /// <summary>
    /// Retorna se o inquilino encontra-se suspenso operacionalmente.
    /// </summary>
    public bool IsSuspended => string.Equals(Status, "Suspended", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Retorna se o inquilino encontra-se ativo.
    /// </summary>
    public bool IsActive => string.Equals(Status, "Active", StringComparison.OrdinalIgnoreCase);
}
