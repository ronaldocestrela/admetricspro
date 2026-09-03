namespace WebApp.Models;

/// <summary>
/// Modelo de visualização para itens tabulares do diretório de inquilinos.
/// </summary>
/// <param name="Id">Identificador único do inquilino.</param>
/// <param name="CompanyName">Razão social ou denominação comercial da empresa.</param>
/// <param name="Cnpj">Cadastro Nacional da Pessoa Jurídica (14 dígitos numéricos).</param>
/// <param name="Subdomain">Subdomínio de roteamento exclusivo do inquilino.</param>
/// <param name="Status">Status operacional no ciclo de vida (ex.: Active, Trial, Delinquent, Suspended, Cancelled).</param>
/// <param name="Tier">Nível do plano contratado (ex.: Trial, Starter, Pro, Enterprise).</param>
/// <param name="SubscriptionExpiresAtUtc">Data e hora em UTC do término da vigência da assinatura ou período de testes.</param>
/// <param name="CreatedAtUtc">Data e hora em UTC de provisionamento inicial do tenant.</param>
/// <param name="WorkspacesCount">Quantidade total de workspaces ativos vinculados ao tenant.</param>
/// <param name="SunkAdSpend">Volume financeiro consolidado de verba gerenciada (Ad Spend) sincronizada no período.</param>
public sealed record TenantDirectoryItemViewModel(
    Guid Id,
    string CompanyName,
    string Cnpj,
    string Subdomain,
    string Status,
    string Tier,
    DateTime? SubscriptionExpiresAtUtc,
    DateTime CreatedAtUtc,
    int WorkspacesCount = 0,
    decimal SunkAdSpend = 0m)
{
    /// <summary>
    /// Formata o CNPJ de 14 dígitos no padrão nacional (XX.XXX.XXX/XXXX-XX).
    /// </summary>
    /// <returns>CNPJ com máscara de formatação.</returns>
    public string FormattedCnpj => FormatCnpj(Cnpj);

    /// <summary>
    /// Aplica a máscara brasileira de CNPJ com 14 dígitos numéricos.
    /// </summary>
    /// <param name="cnpj">String numérica de 14 dígitos.</param>
    /// <returns>String formatada ou original caso o tamanho seja divergente.</returns>
    public static string FormatCnpj(string? cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj) || cnpj.Length != 14)
            return cnpj ?? string.Empty;

        return $"{cnpj[..2]}.{cnpj.Substring(2, 3)}.{cnpj.Substring(5, 3)}/{cnpj.Substring(8, 4)}-{cnpj.Substring(12, 2)}";
    }
}
