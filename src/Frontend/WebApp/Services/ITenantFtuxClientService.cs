using BuildingBlocks.Domain.Primitives;
using Tenants.Application.Ftux.DTOs;
using WebApp.Models;

namespace WebApp.Services;

/// <summary>
/// Contrato do cliente HTTP para monitoramento do checklist de primeiro acesso (FTUX) e ativação de contas demonstrativas.
/// </summary>
public interface ITenantFtuxClientService
{
    /// <summary>
    /// Consulta o estado consolidado de progresso dos 4 passos essenciais do FTUX para o inquilino ativo.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado contendo os indicadores de FTUX ou falha.</returns>
    Task<Result<TenantFtuxStatusDto>> GetFtuxStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Vincula uma conta de anúncios em modo demonstração ao workspace especificado.
    /// </summary>
    /// <param name="model">Dados do workspace e plataforma.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Identificador da conta conectada ou erro semântico.</returns>
    Task<Result<Guid>> ConnectDemoAccountAsync(ConnectDemoAccountModel model, CancellationToken cancellationToken = default);
}
