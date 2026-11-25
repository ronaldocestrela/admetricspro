using BuildingBlocks.Application.Messaging;

namespace SocialPublishing.Application.Publishing.Commands.DispatchDuePublications;

/// <summary>
/// Comando interno do worker para processar jobs de publicação orgânica devidos no tenant atual.
/// </summary>
/// <param name="PublicationJobId">Quando informado, processa apenas este job.</param>
/// <param name="ClaimTokenUtc">Carimbo do claim que deve coincidir com o job em <c>InProgress</c>.</param>
public sealed record DispatchDuePublicationsCommand(
    Guid? PublicationJobId = null,
    DateTime? ClaimTokenUtc = null) : ICommand<DispatchDuePublicationsSummary>;
