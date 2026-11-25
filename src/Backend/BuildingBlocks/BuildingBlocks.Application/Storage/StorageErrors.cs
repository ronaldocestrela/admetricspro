using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Application.Storage;

/// <summary>
/// Códigos estáveis de falha do armazenamento de objetos (MinIO / disco local).
/// </summary>
public static class StorageErrors
{
    /// <summary>
    /// Provedor local ativo; não usado como falha de request HTTP.
    /// </summary>
    public static Error Disabled { get; } =
        Error.Failure("Storage.Disabled", "O provedor de objeto está em modo local.");

    /// <summary>
    /// Objeto ausente quando o chamador esperava conteúdo.
    /// </summary>
    public static Error ObjectNotFound { get; } =
        Error.NotFound("Storage.ObjectNotFound", "O objeto solicitado não foi encontrado.");

    /// <summary>
    /// Falha ao gravar objeto.
    /// </summary>
    public static Error WriteFailed { get; } =
        Error.Failure("Storage.WriteFailed", "Não foi possível gravar o objeto.");

    /// <summary>
    /// Falha ao ler objeto (exceto ausência).
    /// </summary>
    public static Error ReadFailed { get; } =
        Error.Failure("Storage.ReadFailed", "Não foi possível ler o objeto.");

    /// <summary>
    /// Falha ao excluir objeto existente.
    /// </summary>
    public static Error DeleteFailed { get; } =
        Error.Failure("Storage.DeleteFailed", "Não foi possível excluir o objeto.");

    /// <summary>
    /// Chave vazia, malformada ou fora do prefixo do tenant informado.
    /// </summary>
    public static Error KeyInvalid { get; } =
        Error.Validation("Storage.KeyInvalid", "A chave do objeto é inválida para este tenant.");
}