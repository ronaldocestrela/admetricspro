using BuildingBlocks.Domain.Primitives;

namespace Master.Application.Users.Services;

/// <summary>
/// Contrato para inicialização e seed idempotente das credenciais do Super Administrador e papéis corporativos no catálogo Master.
/// </summary>
public interface IMasterIdentitySeeder
{
    /// <summary>
    /// Executa o seed das roles padrão do Backoffice e do usuário SuperAdmin obtido das variáveis de ambiente (.env).
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento da operação assíncrona.</param>
    /// <returns>Resultado indicando sucesso ou falha da rotina de provisionamento inicial.</returns>
    Task<Result> SeedSuperAdminAsync(CancellationToken cancellationToken = default);
}
