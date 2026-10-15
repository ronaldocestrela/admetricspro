using System.Text.Json;
using BuildingBlocks.Domain.Primitives;

namespace Integrations.Domain.Meta;

/// <summary>Resposta sanitizada de criação ou alteração na Meta.</summary>
/// <param name="ExternalId">Identificador externo criado ou alterado.</param>
/// <param name="Success">Confirmação da operação.</param>
public sealed record MetaWriteResult(string ExternalId, bool Success);

/// <summary>Operações oficiais de gerenciamento, sempre contextualizadas pela conta vinculada.</summary>
public interface IMetaManagementApi
{
    /// <summary>Consulta opções dinâmicas pelos tipos oficiais de targeting.</summary>
    /// <param name="type">adinterest, adlocale ou adgeolocation.</param>
    /// <param name="query">Termo de pesquisa.</param>
    /// <param name="token">Token backend.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Opções oficiais sem catálogo inventado localmente.</returns>
    Task<Result<IReadOnlyList<JsonElement>>> SearchTargetingAsync(string type, string query, string token, CancellationToken cancellationToken);
    /// <summary>Consulta detalhes após confirmar propriedade da conta.</summary>
    /// <param name="accountId">ID externo canônico da conta autorizada.</param>
    /// <param name="resource">campaigns, adsets, ads ou adcreatives.</param>
    /// <param name="externalId">Identificador externo do objeto.</param>
    /// <param name="token">Token backend.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Objeto com campos explicitamente selecionados.</returns>
    Task<Result<JsonElement>> GetAsync(string accountId, string resource, string externalId, string token, CancellationToken cancellationToken);

    /// <summary>Cria ou altera um recurso, sem repetir POST automaticamente.</summary>
    /// <param name="accountId">Conta proprietária.</param>
    /// <param name="resource">Tipo de recurso suportado.</param>
    /// <param name="externalId">ID do objeto para edição, nulo para criação.</param>
    /// <param name="data">Campos oficiais previamente validados.</param>
    /// <param name="token">Token backend.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Confirmação ou falha sanitizada.</returns>
    Task<Result<MetaWriteResult>> WriteAsync(string accountId, string resource, string? externalId, JsonElement data, string token, CancellationToken cancellationToken);

    /// <summary>Lista ativos elegíveis da conta autorizada.</summary>
    /// <param name="accountId">Conta proprietária.</param>
    /// <param name="asset">Tipo de ativo permitido.</param>
    /// <param name="token">Token backend.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Ativos com IDs e nomes, sem dados de público individual.</returns>
    Task<Result<IReadOnlyList<JsonElement>>> ListAssetsAsync(string accountId, string asset, string token, CancellationToken cancellationToken);
}
