using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Acessor de escopo para resolução da instância de <see cref="TenantDbContext"/> correspondente ao inquilino ativo.
/// Garante que repositórios e unidades de trabalho compartilhem o mesmo contexto durante o ciclo de vida da requisição.
/// </summary>
public interface ITenantDbContextAccessor
{
    /// <summary>
    /// Obtém de forma assíncrona o <see cref="TenantDbContext"/> resolvido para a conexão do inquilino atual.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado com a instância de contexto ou falha semântica de resolução.</returns>
    Task<Result<TenantDbContext>> GetDbContextAsync(CancellationToken cancellationToken = default);
}
