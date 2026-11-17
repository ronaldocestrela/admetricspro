using BuildingBlocks.Application.Messaging;

namespace SocialPublishing.Application.Publishing.Commands.DispatchDuePublications;

/// <summary>
/// Comando interno do worker para processar jobs de publicação orgânica devidos no tenant atual.
/// </summary>
public sealed record DispatchDuePublicationsCommand : ICommand<DispatchDuePublicationsSummary>;
