using BuildingBlocks.Domain.Primitives;

namespace Integrations.Domain.Meta;

/// <summary>Conta de anúncios acessível pelo token de um workspace.</summary>
/// <param name="Id">ID canônico act_.</param>
/// <param name="Name">Nome da conta.</param>
/// <param name="Currency">Moeda original.</param>
/// <param name="TimeZone">Fuso da conta, se fornecido.</param>
/// <param name="AccountStatus">Estado externo informado pela Meta.</param>
/// <param name="Tasks">Tarefas autorizadas para o usuário.</param>
public sealed record MetaAdAccount(string Id, string Name, string Currency, string? TimeZone, int? AccountStatus, IReadOnlyList<string> Tasks);

/// <summary>Consulta oficial de contas acessíveis; não modifica publicidade.</summary>
public interface IMetaAdAccountsApi
{
    /// <summary>Lista todas as páginas de contas da autorização.</summary>
    /// <param name="accessToken">Token descriptografado apenas no backend.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Contas autorizadas ou falha sanitizada.</returns>
    Task<Result<IReadOnlyList<MetaAdAccount>>> ListAsync(string accessToken, CancellationToken cancellationToken);
}
