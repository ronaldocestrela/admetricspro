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
    /// Erro quando o tipo MIME não é permitido ou não combina com o formato.
    /// </summary>
    public static Error MediaMimeInvalid { get; } =
        Error.Validation("Social.MediaMimeInvalid", "O tipo MIME não é permitido ou não é compatível com o formato informado.");

    /// <summary>
    /// Erro quando a URL base pública de mídia orgânica não está configurada.
    /// </summary>
    public static Error MediaPublicBaseUrlMissing { get; } =
        Error.Validation("Social.MediaPublicBaseUrlMissing", "SocialPublishing:Media:PublicBaseUrl é obrigatório para publicação orgânica.");

    /// <summary>
    /// Erro quando o cabeçalho do arquivo não permite ler dimensões ou duração.
    /// </summary>
    public static Error MediaUnreadable { get; } =
        Error.Validation("Social.MediaUnreadable", "Não foi possível ler metadados da mídia enviada.");

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
    /// Erro quando o workspace não possui DNA de marca persistido.
    /// </summary>
    public static Error BrandVoiceNotFound { get; } =
        Error.NotFound("Social.BrandVoiceNotFound", "Nenhum perfil de voz de marca foi encontrado para este workspace.");

    /// <summary>
    /// Erro quando a data final do plano é anterior à data inicial.
    /// </summary>
    public static Error PlanRangeInvalid { get; } =
        Error.Validation("Social.PlanRangeInvalid", "A data final do cronograma deve ser igual ou posterior à data inicial.");

    /// <summary>
    /// Erro quando a quantidade de posts por semana está fora do intervalo permitido (1–7).
    /// </summary>
    public static Error PostsPerWeekInvalid { get; } =
        Error.Validation("Social.PostsPerWeekInvalid", "A quantidade de posts por semana deve estar entre 1 e 7.");

    /// <summary>
    /// Erro quando o identificador IANA do fuso horário é inválido ou está vazio.
    /// </summary>
    public static Error TimeZoneInvalid { get; } =
        Error.Validation("Social.TimeZoneInvalid", "O fuso horário informado não é um identificador IANA válido.");

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

    /// <summary>
    /// Erro quando o state OAuth está ausente, expirado, adulterado ou não corresponde ao tenant/workspace.
    /// </summary>
    public static Error OAuthStateInvalid { get; } =
        Error.Validation("Social.OAuthStateInvalid", "O state de autorização orgânica é inválido ou expirou.");

    /// <summary>
    /// Erro quando a URI de redirecionamento do fluxo OAuth é inválida.
    /// </summary>
    public static Error RedirectUriInvalid { get; } =
        Error.Validation("Social.RedirectUriInvalid", "A URI de redirecionamento deve ser absoluta e usar HTTPS (ou HTTP em loopback).");

    /// <summary>
    /// Erro quando o tenant não está resolvido no contexto da requisição.
    /// </summary>
    public static Error TenantRequired { get; } =
        Error.Unauthorized("Social.TenantRequired", "O inquilino deve estar resolvido para iniciar a autorização orgânica.");

    /// <summary>
    /// Erro quando credenciais ou identificadores do aplicativo OAuth orgânico não estão configurados.
    /// </summary>
    public static Error OAuthConfigurationMissing { get; } =
        Error.Validation("Social.OAuthConfigurationMissing", "A configuração OAuth orgânica da rede está incompleta.");

    /// <summary>
    /// Erro quando a troca do código de autorização pelo provedor OAuth falha.
    /// </summary>
    public static Error OAuthExchangeFailed { get; } =
        Error.Validation("Social.OAuthExchangeFailed", "Não foi possível concluir a autorização orgânica com a rede. Inicie uma nova conexão.");

    /// <summary>
    /// Erro quando o propósito criptográfico não corresponde ao esperado para a conexão.
    /// </summary>
    public static Error VaultPurposeMismatch { get; } =
        Error.Validation("Social.VaultPurposeMismatch", "O segredo não pertence ao contexto criptográfico informado.");

    /// <summary>
    /// Erro quando nenhuma conta publicável foi encontrada após o consentimento.
    /// </summary>
    public static Error OrganicAccountNotFound { get; } =
        Error.Validation("Social.OrganicAccountNotFound", "Nenhuma conta elegível para publicação orgânica foi encontrada na rede.");

    /// <summary>
    /// Erro quando há mais de uma conta elegível e a seleção automática não é permitida.
    /// </summary>
    public static Error OrganicAccountAmbiguous { get; } =
        Error.Validation("Social.OrganicAccountAmbiguous", "Múltiplas contas elegíveis foram encontradas. Selecione uma conta antes de conectar.");

    /// <summary>
    /// Erro quando a legenda gerada contém termo proibido do perfil de marca.
    /// </summary>
    public static Error CopyContainsForbiddenTerm { get; } =
        Error.Validation("Social.CopyContainsForbiddenTerm", "A legenda gerada contém termo proibido pelo DNA de marca.");

    /// <summary>
    /// Erro quando o provedor de IA falha ou retorna resposta inválida.
    /// </summary>
    public static Error CopyGenerationFailed { get; } =
        Error.Failure("Social.CopyGenerationFailed", "Não foi possível gerar a cópia com o provedor de IA. Tente novamente.");

    /// <summary>
    /// Erro quando a configuração do provedor de IA social está incompleta.
    /// </summary>
    public static Error AiConfigurationMissing { get; } =
        Error.Validation("Social.AiConfigurationMissing", "A configuração SocialAi (BaseUrl, Model ou ApiKey) está incompleta.");

    /// <summary>
    /// Erro quando não há conexão orgânica ativa para a rede do slot.
    /// </summary>
    public static Error ConnectionRequired { get; } =
        Error.Validation("Social.ConnectionRequired", "A conexão orgânica é obrigatória.");

    /// <summary>
    /// Erro quando a conexão orgânica não existe para o workspace informado.
    /// </summary>
    public static Error ConnectionNotFound { get; } =
        Error.NotFound("Social.ConnectionNotFound", "Conexão orgânica não encontrada para este workspace.");

    /// <summary>
    /// Erro quando a geração do plano editorial já está em andamento para a mesma chave de idempotência.
    /// </summary>
    public static Error PlanGenerationInProgress { get; } =
        Error.Conflict("Social.PlanGenerationInProgress", "A geração do plano editorial já está em andamento para este intervalo e semente.");

    /// <summary>
    /// Erro quando o limite superior da consulta de posts é anterior ao inferior.
    /// </summary>
    public static Error PostsRangeInvalid { get; } =
        Error.Validation("Social.PostsRangeInvalid", "O parâmetro 'to' deve ser igual ou posterior a 'from'.");

    /// <summary>
    /// Erro quando o post orgânico não existe no workspace informado.
    /// </summary>
    public static Error PostNotFound { get; } =
        Error.NotFound("Social.PostNotFound", "Post orgânico não encontrado para este workspace.");

    /// <summary>
    /// Erro quando um post agendado recebe horário anterior ao instante atual.
    /// </summary>
    public static Error ScheduleInThePast { get; } =
        Error.Validation("Social.ScheduleInThePast", "Posts agendados não podem ser movidos para um horário no passado.");

    /// <summary>
    /// Erro quando o instante informado não está em UTC.
    /// </summary>
    public static Error ScheduleInstantNotUtc { get; } =
        Error.Validation("Social.ScheduleInstantNotUtc", "O horário agendado deve ser informado em UTC.");

    /// <summary>
    /// Erro quando a operação exige usuário autenticado e nenhum está no contexto.
    /// </summary>
    public static Error UserRequired { get; } =
        Error.Unauthorized("Social.UserRequired", "Usuário autenticado é obrigatório para esta operação.");

    /// <summary>
    /// Erro quando já existe job de publicação para o mesmo slot (chave de idempotência).
    /// </summary>
    public static Error PublicationJobAlreadyExists { get; } =
        Error.Conflict("Social.PublicationJobAlreadyExists", "Já existe um job de publicação para este horário do post.");

    /// <summary>
    /// Erro quando o job está em processamento e bloqueia reagendamento.
    /// </summary>
    public static Error PublicationJobInProgress { get; } =
        Error.Conflict("Social.PublicationJobInProgress", "A publicação deste post está em andamento e não pode ser reagendada.");

    /// <summary>
    /// Erro quando o job não pode ser reivindicado no status atual.
    /// </summary>
    public static Error PublicationJobNotClaimable { get; } =
        Error.Conflict("Social.PublicationJobNotClaimable", "O job de publicação não está disponível para reivindicação.");

    /// <summary>
    /// Erro quando o job não pode ser cancelado no status atual.
    /// </summary>
    public static Error PublicationJobNotCancellable { get; } =
        Error.Conflict("Social.PublicationJobNotCancellable", "O job de publicação não pode ser cancelado no status atual.");

    /// <summary>
    /// Erro quando a transição de status do job não é permitida.
    /// </summary>
    public static Error PublicationJobInvalidTransition { get; } =
        Error.Conflict("Social.PublicationJobInvalidTransition", "A transição de status do job de publicação não é permitida.");

    /// <summary>
    /// Erro quando a rede respondeu 429 ou quota esgotada.
    /// </summary>
    public static Error PublishRateLimited { get; } =
        Error.Failure("Social.PublishRateLimited", "A rede limitou a taxa de publicação. O job será reagendado para retentativa.");

    /// <summary>
    /// Erro quando a publicação falhou de forma definitiva ou funcional na rede.
    /// </summary>
    public static Error PublishFailed { get; } =
        Error.Failure("Social.PublishFailed", "Não foi possível publicar o conteúdo na rede.");

    /// <summary>
    /// Erro quando o formato não é suportado por este adaptador de plataforma.
    /// </summary>
    public static Error PublishFormatUnsupported { get; } =
        Error.Validation("Social.PublishFormatUnsupported", "O formato de post não é suportado por este adaptador de publicação.");

    /// <summary>
    /// Erro quando o identificador externo da publicação é obrigatório e está ausente.
    /// </summary>
    public static Error PublishedExternalIdRequired { get; } =
        Error.Validation("Social.PublishedExternalIdRequired", "O identificador externo da publicação é obrigatório.");

    /// <summary>
    /// Erro quando o token de aprovação pública é inválido, expirado ou revogado.
    /// </summary>
    public static Error ApprovalTokenInvalid { get; } =
        Error.NotFound("Social.ApprovalTokenInvalid", "O link de aprovação não está disponível.");

    /// <summary>
    /// Erro quando a expiração do link de aprovação é inválida ou ausente.
    /// </summary>
    public static Error ApprovalLinkExpirationInvalid { get; } =
        Error.Validation("Social.ApprovalLinkExpirationInvalid", "A data de expiração do link deve ser futura em UTC.");

    /// <summary>
    /// Erro quando a decisão pública exige comentário e ele está ausente.
    /// </summary>
    public static Error ApprovalCommentRequired { get; } =
        Error.Validation("Social.ApprovalCommentRequired", "Informe um comentário ao solicitar alterações.");

    /// <summary>
    /// Erro quando nenhum post foi selecionado na decisão pública.
    /// </summary>
    public static Error ApprovalPostSelectionRequired { get; } =
        Error.Validation("Social.ApprovalPostSelectionRequired", "Selecione ao menos um post para decidir.");

    /// <summary>
    /// Erro quando não há posts publicados com impressões suficientes na janela para calcular tração.
    /// </summary>
    public static Error OrganicTractionInsufficientData { get; } =
        Error.Validation(
            "OrganicTraction.InsufficientData",
            "Não há dados suficientes de impressões na janela para detectar tração orgânica.");

    /// <summary>
    /// Erro quando já existe sugestão de impulsionamento aberta para o mesmo post orgânico.
    /// </summary>
    public static Error BoostAlreadyRequested { get; } =
        Error.Conflict(
            "Social.BoostAlreadyRequested",
            "Já existe uma sugestão de impulsionamento aberta para este post orgânico.");

    /// <summary>
    /// Erro quando a leitura de insights orgânicos na rede falha.
    /// </summary>
    public static Error InsightsReadFailed { get; } =
        Error.Failure(
            "Social.InsightsReadFailed",
            "Não foi possível obter métricas orgânicas da rede para este post.");
}
