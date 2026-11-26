using BuildingBlocks.Application.Messaging;
using Tenants.Application.Branding.DTOs;

namespace Tenants.Application.Branding.Commands.UploadTenantBrandAsset;

/// <summary>
/// Comando para enviar logo ou favicon ao bucket de branding.
/// </summary>
/// <param name="Slot">Slot do ativo.</param>
/// <param name="FileName">Nome original do arquivo.</param>
/// <param name="ContentType">MIME declarado pelo cliente.</param>
/// <param name="Content">Conteúdo binário.</param>
/// <param name="RequestOrigin">Origem HTTPS da requisição (montagem da URL pública).</param>
public sealed record UploadTenantBrandAssetCommand(
    TenantBrandAssetSlot Slot,
    string? FileName,
    string ContentType,
    Stream Content,
    string? RequestOrigin) : ICommand<TenantBrandingDetailsDto>;