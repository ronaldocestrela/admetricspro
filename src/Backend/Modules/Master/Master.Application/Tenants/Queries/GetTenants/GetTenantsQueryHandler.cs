using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Repositories;
using Master.Application.Tenants.Queries.GetTenantDetails;

namespace Master.Application.Tenants.Queries.GetTenants;

/// <summary>
/// Manipulador de consulta responsável por retornar todos os inquilinos registrados.
/// </summary>
public sealed class GetTenantsQueryHandler : IQueryHandler<GetTenantsQuery, IReadOnlyList<TenantDetailsResponse>>
{
    private readonly ITenantReadOnlyRepository _readOnlyRepository;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GetTenantsQueryHandler"/>.
    /// </summary>
    /// <param name="readOnlyRepository">Repositório de leitura somente-leitura de inquilinos.</param>
    public GetTenantsQueryHandler(ITenantReadOnlyRepository readOnlyRepository)
    {
        _readOnlyRepository = readOnlyRepository ?? throw new ArgumentNullException(nameof(readOnlyRepository));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<TenantDetailsResponse>>> Handle(GetTenantsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var tenants = await _readOnlyRepository.GetAllAsync(cancellationToken);
        return Result<IReadOnlyList<TenantDetailsResponse>>.Success(tenants);
    }
}
