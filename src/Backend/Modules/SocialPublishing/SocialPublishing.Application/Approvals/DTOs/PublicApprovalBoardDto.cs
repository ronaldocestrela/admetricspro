namespace SocialPublishing.Application.Approvals.DTOs;

/// <summary>
/// DTO composto na Web API com posts e branding white-label para o portal público.
/// </summary>
/// <param name="Posts">Posts aguardando decisão do cliente.</param>
/// <param name="PrimaryColor">Cor primária da agência.</param>
/// <param name="LogoUrl">URL do logotipo claro, se configurado.</param>
/// <param name="AgencyName">Nome exibido da agência.</param>
public sealed record PublicApprovalBoardDto(
    IReadOnlyList<PublicApprovalPostDto> Posts,
    string PrimaryColor,
    string? LogoUrl,
    string AgencyName);
