using BuildingBlocks.Domain.Primitives;
using Master.Application.Tenants.Queries.GetTenantCustomDomain;

namespace WebApp.Services;

/// <summary>
/// Contrato de serviço cliente para operações de domínio CNAME personalizado da agência via Web API.
/// </summary>
public interface ITenantCnameClientService
{
    /// <summary>
    /// Obtém as configurações e status do domínio customizado CNAME do inquilino ativo.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Dados e status do CNAME.</returns>
    Task<Result<TenantCustomDomainDto>> GetCnameDetailsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Configura ou atualiza o domínio CNAME personalizado do inquilino.
    /// </summary>
    /// <param name="customDomain">Domínio personalizado (ex: relatorios.agencia.com.br).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    Task<Result> ConfigureCnameAsync(string customDomain, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove o domínio CNAME personalizado vinculado ao inquilino.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da remoção.</returns>
    Task<Result> RemoveCnameAsync(CancellationToken cancellationToken = default);
}
