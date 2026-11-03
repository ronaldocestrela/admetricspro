using BuildingBlocks.Domain.Primitives;

namespace SocialPublishing.Domain;

/// <summary>
/// Catálogo estável de erros de negócio do bounded context Social Publishing.
/// </summary>
public static class SocialErrors
{
    /// <summary>
    /// Erro quando o identificador do workspace é vazio.
    /// </summary>
    public static Error WorkspaceRequired { get; } =
        Error.Validation("Social.WorkspaceRequired", "O identificador do workspace é obrigatório.");

    /// <summary>
    /// Erro quando a plataforma orgânica não é suportada.
    /// </summary>
    public static Error PlatformUnsupported { get; } =
        Error.Validation("Social.PlatformUnsupported", "A plataforma orgânica informada não é suportada.");

    /// <summary>
    /// Erro quando a conexão foi revogada ou o token não pode ser rotacionado.
    /// </summary>
    public static Error TokenRevoked { get; } =
        Error.Conflict("Social.TokenRevoked", "A conexão orgânica foi revogada ou o token foi rejeitado pela rede.");

    /// <summary>
    /// Erro quando a mídia não atende à proporção exigida pelo formato.
    /// </summary>
    public static Error MediaAspectInvalid { get; } =
        Error.Validation("Social.MediaAspectInvalid", "A mídia não atende à proporção exigida para este formato.");

    /// <summary>
    /// Erro quando a legenda excede o teto da rede.
    /// </summary>
    public static Error CaptionTooLong { get; } =
        Error.Validation("Social.CaptionTooLong", "A legenda excede o tamanho máximo permitido pela rede.");

    /// <summary>
    /// Erro quando as hashtags violam a política configurada.
    /// </summary>
    public static Error HashtagPolicyViolated { get; } =
        Error.Validation("Social.HashtagPolicyViolated", "As hashtags violam a política de duplicidade, formato ou quantidade.");

    /// <summary>
    /// Erro quando os pesos dos pilares são inválidos.
    /// </summary>
    public static Error PillarWeightsInvalid { get; } =
        Error.Validation("Social.PillarWeightsInvalid", "Os pesos dos pilares devem somar 100 e nenhum pode ser negativo.");

    /// <summary>
    /// Erro quando o post não pode ser reagendado no status atual.
    /// </summary>
    public static Error SlotNotReschedulable { get; } =
        Error.Conflict("Social.SlotNotReschedulable", "O post não pode ser reagendado no status atual.");

    /// <summary>
    /// Erro quando a transição de status não é permitida.
    /// </summary>
    public static Error InvalidTransition { get; } =
        Error.Conflict("Social.InvalidTransition", "A transição de status solicitada não é permitida.");

    /// <summary>
    /// Erro quando a quantidade de itens do carrossel está fora do intervalo permitido.
    /// </summary>
    public static Error CarouselItemCountInvalid { get; } =
        Error.Validation("Social.CarouselItemCountInvalid", "O carrossel deve conter entre 2 e 10 itens de mídia.");

    /// <summary>
    /// Erro quando a aplicação de migrações do schema social falha tecnicamente.
    /// </summary>
    public static Error MigrationFailed { get; } =
        Error.Failure("Social.MigrationFailed", "Falha ao aplicar migrações do schema social no banco do tenant.");
}
