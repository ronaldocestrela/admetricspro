using BuildingBlocks.Application.Messaging;
using Tenants.Application.Branding.DTOs;

namespace Tenants.Application.Branding.Commands.DeleteTenantBrandAsset;

/// <summary>
/// Remove o objeto e metadados de um slot de branding.
/// </summary>
/// <param name="Slot">Slot do ativo.</param>
/// <param name="RequestOrigin">Origem HTTPS para o DTO de retorno.</param>
public sealed record DeleteTenantBrandAssetCommand(
    TenantBrandAssetSlot Slot,
    string? RequestOrigin) : ICommand<TenantBrandingDetailsDto>;