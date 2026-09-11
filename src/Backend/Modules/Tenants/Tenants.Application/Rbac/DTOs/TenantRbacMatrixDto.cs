namespace Tenants.Application.Rbac.DTOs;

/// <summary>
/// DTO representando a matriz completa de papéis e permissões canônicas do inquilino.
/// </summary>
/// <param name="Roles">Mapeamento de papéis para suas respectivas permissões concedidas.</param>
/// <param name="AllPermissions">Lista exaustiva de todas as permissões do catálogo.</param>
public sealed record TenantRbacMatrixDto(
    IReadOnlyDictionary<string, IReadOnlyList<string>> Roles,
    IReadOnlyList<string> AllPermissions);
