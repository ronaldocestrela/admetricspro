namespace Tenants.Application.Branding;

/// <summary>
/// Resultado de leitura de um ativo de branding.
/// </summary>
/// <param name="Content">Fluxo legível do objeto.</param>
/// <param name="ContentType">Tipo MIME gravado.</param>
public sealed record TenantBrandAssetReadResult(Stream Content, string ContentType);