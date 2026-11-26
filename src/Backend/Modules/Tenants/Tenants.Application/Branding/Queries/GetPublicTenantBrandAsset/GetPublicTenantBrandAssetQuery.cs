using BuildingBlocks.Application.Messaging;
using Tenants.Application.Branding;

namespace Tenants.Application.Branding.Queries.GetPublicTenantBrandAsset;

/// <summary>
/// Consulta pública para stream de ativo de branding por tenant e slot.
/// </summary>
/// <param name="TenantId">Identificador do tenant (formato N na rota).</param>
/// <param name="Slot">Slot do ativo.</param>
public sealed record GetPublicTenantBrandAssetQuery(Guid TenantId, TenantBrandAssetSlot Slot)
    : IQuery<TenantBrandAssetReadResult>;