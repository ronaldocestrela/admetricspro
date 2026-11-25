using BuildingBlocks.Application.Messaging;

namespace SocialPublishing.Application.Publishing.Commands.ReleasePublicationClaim;

/// <summary>
/// Libera o claim de um job orgânico quando o kill switch impede a publicação externa.
/// </summary>
/// <param name="PublicationJobId">Job em <c>InProgress</c>.</param>
/// <param name="ClaimTokenUtc">Carimbo que deve coincidir com <see cref="Domain.Publications.PublicationJob.ClaimedAtUtc"/>.</param>
public sealed record ReleasePublicationClaimCommand(Guid PublicationJobId, DateTime ClaimTokenUtc) : ICommand;