namespace WebApi.Models;

/// <summary>
/// Modelo de requisição para configuração de domínio customizado CNAME do inquilino.
/// </summary>
/// <param name="CustomDomain">Nome do domínio personalizado FQDN (ex: relatorios.agencia.com.br).</param>
public sealed record ConfigureTenantCustomDomainApiRequest(string CustomDomain);
