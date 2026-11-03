using BuildingBlocks.Domain.Primitives;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SocialPublishing.Application.Brand.Commands.UpsertBrandVoice;
using SocialPublishing.Application.Brand.DTOs;
using SocialPublishing.Application.Brand.Queries.GetBrandVoice;
using SocialPublishing.Application.Connections.Commands.CompleteOrganicAuthorization;
using SocialPublishing.Application.Connections.Commands.RevokeOrganicConnection;
using SocialPublishing.Application.Connections.DTOs;
using SocialPublishing.Application.Connections.Queries.ListOrganicConnections;
using SocialPublishing.Application.Connections.Queries.StartOrganicAuthorization;
using SocialPublishing.Application.Media.Commands.RegisterOrganicMedia;
using SocialPublishing.Application.Media.DTOs;
using SocialPublishing.Application.Options;
using SocialPublishing.Application.Planning.Commands.GenerateEditorialPlan;
using SocialPublishing.Application.Planning.DTOs;
using SocialPublishing.Application.Posts.Commands.ApprovePost;
using SocialPublishing.Application.Posts.Commands.SchedulePost;
using SocialPublishing.Application.Posts.Commands.RejectPost;
using SocialPublishing.Application.Posts.Commands.ReschedulePost;
using SocialPublishing.Application.Posts.Commands.SubmitPostForApproval;
using SocialPublishing.Application.Posts.DTOs;
using SocialPublishing.Application.Posts.Queries.ListOrganicPosts;
using SocialPublishing.Application.Status.DTOs;
using SocialPublishing.Application.Status.Queries.GetSocialModuleStatus;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Brand;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Posts;
using Tenants.Application.Workspaces.Queries.GetWorkspaceById;

namespace WebApi.Controllers.v1;

/// <summary>
/// Controlador HTTP do bounded context Social Publishing (publicação orgânica e calendário).
/// </summary>
[Authorize(AuthenticationSchemes = "Tenant")]
[ApiController]
[Route("api/v1/social")]
public sealed class SocialController : ControllerBase
{
    private readonly ISender _sender;
    private readonly OrganicMediaOptions _mediaOptions;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="SocialController"/>.
    /// </summary>
    /// <param name="sender">Mediador in-memory MediatR.</param>
    /// <param name="mediaOptions">Limites de upload de mídia orgânica.</param>
    public SocialController(ISender sender, IOptions<OrganicMediaOptions> mediaOptions)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _mediaOptions = mediaOptions?.Value ?? throw new ArgumentNullException(nameof(mediaOptions));
    }

    /// <summary>
    /// Obtém o resumo operacional do módulo (conexões orgânicas e posts agendados) para o workspace informado.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace cliente.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Contagens agregadas ou falha de negócio/autorização.</returns>
    [HttpGet("status")]
    [EndpointSummary("Obtém o resumo operacional do módulo Social Publishing para um workspace")]
    [ProducesResponseType(typeof(Result<SocialModuleStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<SocialModuleStatusDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<SocialModuleStatusDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result<SocialModuleStatusDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Result<SocialModuleStatusDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result<SocialModuleStatusDto>>> GetStatus(
        [FromQuery] Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
        {
            return BadRequest(Result<SocialModuleStatusDto>.Failure(
                SocialPublishing.Domain.SocialErrors.WorkspaceRequired));
        }

        var workspaceResult = await _sender.Send(new GetWorkspaceByIdQuery(workspaceId), cancellationToken);
        if (workspaceResult.IsFailure)
        {
            return workspaceResult.Error.Type switch
            {
                ErrorType.NotFound => NotFound(workspaceResult),
                ErrorType.Unauthorized => Unauthorized(workspaceResult),
                _ => BadRequest(workspaceResult)
            };
        }

        var result = await _sender.Send(new GetSocialModuleStatusQuery(workspaceId), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.Unauthorized => Unauthorized(result),
                ErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, result),
                ErrorType.NotFound => NotFound(result),
                _ => BadRequest(result)
            };
        }

        return Ok(result);
    }

    /// <summary>
    /// Inicia o fluxo OAuth orgânico para Instagram, Facebook Page ou TikTok Content Posting.
    /// </summary>
    /// <param name="request">Workspace, plataforma e URI de retorno.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>URL de autorização e state anti-CSRF (sem segredos).</returns>
    [HttpPost("connections/authorize")]
    [EndpointSummary("Inicia autorização OAuth orgânica para uma rede social")]
    [ProducesResponseType(typeof(Result<StartOrganicAuthorizationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<StartOrganicAuthorizationResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<StartOrganicAuthorizationResponse>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Result<StartOrganicAuthorizationResponse>>> AuthorizeOrganicConnection(
        [FromBody] StartOrganicAuthorizationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return BadRequest(Result<StartOrganicAuthorizationResponse>.Failure(SocialErrors.WorkspaceRequired));
        }

        var workspaceResult = await _sender.Send(new GetWorkspaceByIdQuery(request.WorkspaceId), cancellationToken);
        if (workspaceResult.IsFailure)
        {
            return workspaceResult.Error.Type switch
            {
                ErrorType.NotFound => NotFound(workspaceResult),
                ErrorType.Unauthorized => Unauthorized(workspaceResult),
                _ => BadRequest(workspaceResult)
            };
        }

        var result = await _sender.Send(
            new StartOrganicAuthorizationQuery(request.WorkspaceId, request.Platform, request.RedirectUri),
            cancellationToken);

        return MapSocialConnectionResult(result);
    }

    /// <summary>
    /// Processa o callback OAuth orgânico, troca o código por tokens cifrados e persiste a conexão.
    /// </summary>
    /// <param name="request">Código, state e URI de retorno do provedor.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Metadados da conexão criada ou atualizada (sem tokens).</returns>
    [HttpPost("connections/callback")]
    [EndpointSummary("Conclui callback OAuth orgânico e grava conexão cifrada no cofre do tenant")]
    [ProducesResponseType(typeof(Result<CompleteOrganicAuthorizationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<CompleteOrganicAuthorizationResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<CompleteOrganicAuthorizationResponse>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Result<CompleteOrganicAuthorizationResponse>>> CompleteOrganicAuthorization(
        [FromBody] CompleteOrganicAuthorizationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return BadRequest(Result<CompleteOrganicAuthorizationResponse>.Failure(SocialErrors.WorkspaceRequired));
        }

        var workspaceResult = await _sender.Send(new GetWorkspaceByIdQuery(request.WorkspaceId), cancellationToken);
        if (workspaceResult.IsFailure)
        {
            return workspaceResult.Error.Type switch
            {
                ErrorType.NotFound => NotFound(workspaceResult),
                ErrorType.Unauthorized => Unauthorized(workspaceResult),
                _ => BadRequest(workspaceResult)
            };
        }

        var result = await _sender.Send(
            new CompleteOrganicAuthorizationCommand(
                request.WorkspaceId,
                request.Code,
                request.State,
                request.RedirectUri,
                request.ProviderError),
            cancellationToken);

        return MapSocialConnectionResult(result);
    }

    /// <summary>
    /// Lista conexões orgânicas de um workspace sem expor tokens.
    /// </summary>
    /// <param name="workspaceId">Workspace proprietário.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Metadados das conexões ou falha de negócio/autorização.</returns>
    [HttpGet("connections")]
    [EndpointSummary("Lista conexões orgânicas de um workspace (sem segredos)")]
    [ProducesResponseType(typeof(Result<IReadOnlyList<OrganicConnectionSummaryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<IReadOnlyList<OrganicConnectionSummaryDto>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<IReadOnlyList<OrganicConnectionSummaryDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result<IReadOnlyList<OrganicConnectionSummaryDto>>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Result<IReadOnlyList<OrganicConnectionSummaryDto>>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result<IReadOnlyList<OrganicConnectionSummaryDto>>>> ListOrganicConnections(
        [FromQuery] Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
        {
            return BadRequest(Result<IReadOnlyList<OrganicConnectionSummaryDto>>.Failure(SocialErrors.WorkspaceRequired));
        }

        var workspaceResult = await _sender.Send(new GetWorkspaceByIdQuery(workspaceId), cancellationToken);
        if (workspaceResult.IsFailure)
        {
            return workspaceResult.Error.Type switch
            {
                ErrorType.NotFound => NotFound(Result<IReadOnlyList<OrganicConnectionSummaryDto>>.Failure(workspaceResult.Error)),
                ErrorType.Unauthorized => Unauthorized(Result<IReadOnlyList<OrganicConnectionSummaryDto>>.Failure(workspaceResult.Error)),
                _ => BadRequest(Result<IReadOnlyList<OrganicConnectionSummaryDto>>.Failure(workspaceResult.Error))
            };
        }

        var result = await _sender.Send(new ListOrganicConnectionsQuery(workspaceId), cancellationToken);
        return MapSocialConnectionResult(result);
    }

    /// <summary>
    /// Revoga localmente uma conexão orgânica e invalida o ciphertext persistido.
    /// </summary>
    /// <param name="connectionId">Identificador da conexão.</param>
    /// <param name="workspaceId">Workspace proprietário.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    [HttpDelete("connections/{connectionId:guid}")]
    [EndpointSummary("Revoga conexão orgânica localmente (sem expor segredos)")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Result>> RevokeOrganicConnection(
        Guid connectionId,
        [FromQuery] Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
        {
            return BadRequest(Result.Failure(SocialErrors.WorkspaceRequired));
        }

        var workspaceResult = await _sender.Send(new GetWorkspaceByIdQuery(workspaceId), cancellationToken);
        if (workspaceResult.IsFailure)
        {
            return workspaceResult.Error.Type switch
            {
                ErrorType.NotFound => NotFound(Result.Failure(workspaceResult.Error)),
                ErrorType.Unauthorized => Unauthorized(Result.Failure(workspaceResult.Error)),
                _ => BadRequest(Result.Failure(workspaceResult.Error))
            };
        }

        var result = await _sender.Send(new RevokeOrganicConnectionCommand(workspaceId, connectionId), cancellationToken);
        return MapSocialVoidResult(result);
    }

    /// <summary>
    /// Registra mídia orgânica (metadados no tenant DB, binário no object store).
    /// </summary>
    /// <param name="workspaceId">Workspace proprietário.</param>
    /// <param name="format">Formato de publicação alvo.</param>
    /// <param name="file">Arquivo JPEG, PNG ou MP4.</param>
    /// <param name="carouselAnchorAssetId">Mídia âncora do carrossel, se aplicável.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Metadados do ativo registrado (sem binário).</returns>
    [HttpPost("media")]
    [RequestSizeLimit(110_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 110_000_000)]
    [EndpointSummary("Registra mídia orgânica com validação de proporção e MIME")]
    [ProducesResponseType(typeof(Result<OrganicMediaAssetDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Result<OrganicMediaAssetDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<OrganicMediaAssetDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result<OrganicMediaAssetDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Result<OrganicMediaAssetDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result<OrganicMediaAssetDto>), StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(typeof(Result<OrganicMediaAssetDto>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Result<OrganicMediaAssetDto>>> RegisterMedia(
        [FromForm] Guid workspaceId,
        [FromForm] OrganicPostFormat format,
        IFormFile file,
        [FromForm] Guid? carouselAnchorAssetId = null,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
        {
            return BadRequest(Result<OrganicMediaAssetDto>.Failure(SocialErrors.WorkspaceRequired));
        }

        if (file is null || file.Length < 1)
        {
            return BadRequest(Result<OrganicMediaAssetDto>.Failure(
                Error.Validation("Social.MediaFileRequired", "O arquivo de mídia é obrigatório.")));
        }

        if (file.Length > _mediaOptions.MaxUploadBytes)
        {
            return StatusCode(
                StatusCodes.Status413PayloadTooLarge,
                Result<OrganicMediaAssetDto>.Failure(
                    Error.Validation("Social.MediaTooLarge", "O arquivo excede o tamanho máximo permitido.")));
        }

        var workspaceResult = await _sender.Send(new GetWorkspaceByIdQuery(workspaceId), cancellationToken);
        if (workspaceResult.IsFailure)
        {
            return workspaceResult.Error.Type switch
            {
                ErrorType.NotFound => NotFound(Result<OrganicMediaAssetDto>.Failure(workspaceResult.Error)),
                ErrorType.Unauthorized => Unauthorized(Result<OrganicMediaAssetDto>.Failure(workspaceResult.Error)),
                _ => BadRequest(Result<OrganicMediaAssetDto>.Failure(workspaceResult.Error))
            };
        }

        await using var stream = file.OpenReadStream();
        var result = await _sender.Send(
            new RegisterOrganicMediaCommand(
                workspaceId,
                format,
                file.ContentType,
                stream,
                carouselAnchorAssetId),
            cancellationToken);

        return MapSocialMediaResult(result);
    }

    /// <summary>
    /// Cria ou atualiza o DNA de marca (Brand Voice) do workspace.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace cliente.</param>
    /// <param name="request">Tom, persona, links, termos, emoji e pesos de pilares.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Perfil persistido ou falha de validação.</returns>
    [HttpPut("brand-voice/{workspaceId:guid}")]
    [EndpointSummary("Cria ou atualiza o DNA de marca do workspace para o copiloto social")]
    [ProducesResponseType(typeof(Result<BrandVoiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<BrandVoiceDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<BrandVoiceDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result<BrandVoiceDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Result<BrandVoiceDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result<BrandVoiceDto>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Result<BrandVoiceDto>>> UpsertBrandVoice(
        Guid workspaceId,
        [FromBody] UpsertBrandVoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
        {
            return BadRequest(Result<BrandVoiceDto>.Failure(SocialErrors.WorkspaceRequired));
        }

        var workspaceResult = await _sender.Send(new GetWorkspaceByIdQuery(workspaceId), cancellationToken);
        if (workspaceResult.IsFailure)
        {
            return workspaceResult.Error.Type switch
            {
                ErrorType.NotFound => NotFound(Result<BrandVoiceDto>.Failure(workspaceResult.Error)),
                ErrorType.Unauthorized => Unauthorized(Result<BrandVoiceDto>.Failure(workspaceResult.Error)),
                _ => BadRequest(Result<BrandVoiceDto>.Failure(workspaceResult.Error))
            };
        }

        var result = await _sender.Send(
            new UpsertBrandVoiceCommand(
                workspaceId,
                request.Tone,
                request.Persona,
                request.MandatoryLinks,
                request.ForbiddenTerms,
                request.EmojiPolicy,
                request.HashtagLimit,
                request.AuthorityWeight,
                request.SaleWeight,
                request.ConnectionWeight,
                request.SocialProofWeight,
                request.LanguageCode),
            cancellationToken);

        return MapSocialConnectionResult(result);
    }

    /// <summary>
    /// Obtém o DNA de marca persistido para o workspace.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace cliente.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Perfil de voz de marca ou ausência.</returns>
    [HttpGet("brand-voice/{workspaceId:guid}")]
    [EndpointSummary("Obtém o DNA de marca persistido do workspace")]
    [ProducesResponseType(typeof(Result<BrandVoiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<BrandVoiceDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<BrandVoiceDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result<BrandVoiceDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Result<BrandVoiceDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result<BrandVoiceDto>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Result<BrandVoiceDto>>> GetBrandVoice(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
        {
            return BadRequest(Result<BrandVoiceDto>.Failure(SocialErrors.WorkspaceRequired));
        }

        var workspaceResult = await _sender.Send(new GetWorkspaceByIdQuery(workspaceId), cancellationToken);
        if (workspaceResult.IsFailure)
        {
            return workspaceResult.Error.Type switch
            {
                ErrorType.NotFound => NotFound(Result<BrandVoiceDto>.Failure(workspaceResult.Error)),
                ErrorType.Unauthorized => Unauthorized(Result<BrandVoiceDto>.Failure(workspaceResult.Error)),
                _ => BadRequest(Result<BrandVoiceDto>.Failure(workspaceResult.Error))
            };
        }

        var result = await _sender.Send(new GetBrandVoiceQuery(workspaceId), cancellationToken);
        return MapSocialConnectionResult(result);
    }

    /// <summary>
    /// Materializa um plano editorial em posts orgânicos em Draft (copiloto).
    /// </summary>
    /// <param name="request">Intervalo, fuso, posts por semana e semente.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Posts criados e slots que falharam.</returns>
    [HttpPost("plans:generate")]
    [EndpointSummary("Materializa cronograma editorial em rascunhos de posts orgânicos")]
    [ProducesResponseType(typeof(Result<GeneratedPlanDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<GeneratedPlanDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<GeneratedPlanDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result<GeneratedPlanDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Result<GeneratedPlanDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result<GeneratedPlanDto>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(Result<GeneratedPlanDto>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Result<GeneratedPlanDto>>> GenerateEditorialPlan(
        [FromBody] GenerateEditorialPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return BadRequest(Result<GeneratedPlanDto>.Failure(SocialErrors.WorkspaceRequired));
        }

        var workspaceResult = await _sender.Send(new GetWorkspaceByIdQuery(request.WorkspaceId), cancellationToken);
        if (workspaceResult.IsFailure)
        {
            return workspaceResult.Error.Type switch
            {
                ErrorType.NotFound => NotFound(Result<GeneratedPlanDto>.Failure(workspaceResult.Error)),
                ErrorType.Unauthorized => Unauthorized(Result<GeneratedPlanDto>.Failure(workspaceResult.Error)),
                _ => BadRequest(Result<GeneratedPlanDto>.Failure(workspaceResult.Error))
            };
        }

        var result = await _sender.Send(
            new GenerateEditorialPlanCommand(
                request.WorkspaceId,
                request.RangeStart,
                request.RangeEnd,
                request.TimeZoneId,
                request.PostsPerWeek,
                request.Seed),
            cancellationToken);

        return MapSocialConnectionResult(result);
    }

    /// <summary>
    /// Lista posts orgânicos agendados em um intervalo UTC para alimentar o calendário.
    /// </summary>
    /// <param name="workspaceId">Workspace proprietário.</param>
    /// <param name="from">Limite inferior UTC inclusivo.</param>
    /// <param name="to">Limite superior UTC inclusivo.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Posts no intervalo.</returns>
    [HttpGet("posts")]
    [EndpointSummary("Lista posts orgânicos do workspace em intervalo de agendamento UTC")]
    [ProducesResponseType(typeof(Result<IReadOnlyList<OrganicPostDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<IReadOnlyList<OrganicPostDto>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<IReadOnlyList<OrganicPostDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result<IReadOnlyList<OrganicPostDto>>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Result<IReadOnlyList<OrganicPostDto>>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Result<IReadOnlyList<OrganicPostDto>>>> ListOrganicPosts(
        [FromQuery] Guid workspaceId,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
        {
            return BadRequest(Result<IReadOnlyList<OrganicPostDto>>.Failure(SocialErrors.WorkspaceRequired));
        }

        var workspaceResult = await _sender.Send(new GetWorkspaceByIdQuery(workspaceId), cancellationToken);
        if (workspaceResult.IsFailure)
        {
            return workspaceResult.Error.Type switch
            {
                ErrorType.NotFound => NotFound(Result<IReadOnlyList<OrganicPostDto>>.Failure(workspaceResult.Error)),
                ErrorType.Unauthorized => Unauthorized(Result<IReadOnlyList<OrganicPostDto>>.Failure(workspaceResult.Error)),
                _ => BadRequest(Result<IReadOnlyList<OrganicPostDto>>.Failure(workspaceResult.Error))
            };
        }

        var result = await _sender.Send(new ListOrganicPostsQuery(workspaceId, from, to), cancellationToken);
        return MapSocialConnectionResult(result);
    }

    /// <summary>
    /// Envia um post orgânico para aprovação interna.
    /// </summary>
    /// <param name="id">Identificador do post.</param>
    /// <param name="workspaceId">Workspace proprietário.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Post atualizado ou falha de negócio.</returns>
    [HttpPost("posts/{id:guid}:submit")]
    [EndpointSummary("Envia post orgânico para aprovação interna")]
    [ProducesResponseType(typeof(Result<OrganicPostDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<OrganicPostDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<OrganicPostDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result<OrganicPostDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Result<OrganicPostDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result<OrganicPostDto>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(Result<OrganicPostDto>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Result<OrganicPostDto>>> SubmitPostForApproval(
        Guid id,
        [FromQuery] Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        return await ExecutePostLifecycleCommandAsync(
            workspaceId,
            id,
            wsId => new SubmitPostForApprovalCommand(wsId, id),
            cancellationToken);
    }

    /// <summary>
    /// Aprova um post orgânico em revisão interna.
    /// </summary>
    /// <param name="id">Identificador do post.</param>
    /// <param name="workspaceId">Workspace proprietário.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Post atualizado ou falha de negócio.</returns>
    [HttpPost("posts/{id:guid}:approve")]
    [EndpointSummary("Aprova post orgânico em revisão interna")]
    [ProducesResponseType(typeof(Result<OrganicPostDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<OrganicPostDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<OrganicPostDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result<OrganicPostDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Result<OrganicPostDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result<OrganicPostDto>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(Result<OrganicPostDto>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Result<OrganicPostDto>>> ApprovePost(
        Guid id,
        [FromQuery] Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        return await ExecutePostLifecycleCommandAsync(
            workspaceId,
            id,
            wsId => new ApprovePostCommand(wsId, id),
            cancellationToken);
    }

    /// <summary>
    /// Agenda um post aprovado e registra job pendente na outbox de publicação.
    /// </summary>
    /// <param name="id">Identificador do post.</param>
    /// <param name="workspaceId">Workspace proprietário.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Post atualizado ou falha de negócio.</returns>
    [HttpPost("posts/{id:guid}:schedule")]
    [EndpointSummary("Agenda post aprovado e abre job na outbox")]
    [ProducesResponseType(typeof(Result<OrganicPostDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<OrganicPostDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<OrganicPostDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result<OrganicPostDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Result<OrganicPostDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result<OrganicPostDto>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(Result<OrganicPostDto>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Result<OrganicPostDto>>> SchedulePost(
        Guid id,
        [FromQuery] Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        return await ExecutePostLifecycleCommandAsync(
            workspaceId,
            id,
            wsId => new SchedulePostCommand(wsId, id),
            cancellationToken);
    }

    /// <summary>
    /// Rejeita um post orgânico em revisão interna.
    /// </summary>
    /// <param name="id">Identificador do post.</param>
    /// <param name="workspaceId">Workspace proprietário.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Post atualizado ou falha de negócio.</returns>
    [HttpPost("posts/{id:guid}:reject")]
    [EndpointSummary("Rejeita post orgânico em revisão interna")]
    [ProducesResponseType(typeof(Result<OrganicPostDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<OrganicPostDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<OrganicPostDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result<OrganicPostDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Result<OrganicPostDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result<OrganicPostDto>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(Result<OrganicPostDto>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Result<OrganicPostDto>>> RejectPost(
        Guid id,
        [FromQuery] Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        return await ExecutePostLifecycleCommandAsync(
            workspaceId,
            id,
            wsId => new RejectPostCommand(wsId, id),
            cancellationToken);
    }

    /// <summary>
    /// Reagenda um post orgânico para um novo instante UTC.
    /// </summary>
    /// <param name="id">Identificador do post.</param>
    /// <param name="workspaceId">Workspace proprietário.</param>
    /// <param name="request">Novo horário em UTC.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Post atualizado ou falha de negócio.</returns>
    [HttpPost("posts/{id:guid}:reschedule")]
    [EndpointSummary("Reagenda post orgânico para novo horário UTC")]
    [ProducesResponseType(typeof(Result<OrganicPostDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<OrganicPostDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<OrganicPostDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result<OrganicPostDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Result<OrganicPostDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result<OrganicPostDto>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(Result<OrganicPostDto>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Result<OrganicPostDto>>> ReschedulePost(
        Guid id,
        [FromQuery] Guid workspaceId,
        [FromBody] ReschedulePostRequest request,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
        {
            return BadRequest(Result<OrganicPostDto>.Failure(SocialErrors.WorkspaceRequired));
        }

        var workspaceResult = await _sender.Send(new GetWorkspaceByIdQuery(workspaceId), cancellationToken);
        if (workspaceResult.IsFailure)
        {
            return workspaceResult.Error.Type switch
            {
                ErrorType.NotFound => NotFound(Result<OrganicPostDto>.Failure(workspaceResult.Error)),
                ErrorType.Unauthorized => Unauthorized(Result<OrganicPostDto>.Failure(workspaceResult.Error)),
                _ => BadRequest(Result<OrganicPostDto>.Failure(workspaceResult.Error))
            };
        }

        var result = await _sender.Send(
            new ReschedulePostCommand(workspaceId, id, request.ScheduledAtUtc),
            cancellationToken);
        return MapSocialConnectionResult(result);
    }

    private async Task<ActionResult<Result<OrganicPostDto>>> ExecutePostLifecycleCommandAsync<TCommand>(
        Guid workspaceId,
        Guid postId,
        Func<Guid, TCommand> commandFactory,
        CancellationToken cancellationToken)
        where TCommand : IRequest<Result<OrganicPostDto>>
    {
        if (workspaceId == Guid.Empty)
        {
            return BadRequest(Result<OrganicPostDto>.Failure(SocialErrors.WorkspaceRequired));
        }

        var workspaceResult = await _sender.Send(new GetWorkspaceByIdQuery(workspaceId), cancellationToken);
        if (workspaceResult.IsFailure)
        {
            return workspaceResult.Error.Type switch
            {
                ErrorType.NotFound => NotFound(Result<OrganicPostDto>.Failure(workspaceResult.Error)),
                ErrorType.Unauthorized => Unauthorized(Result<OrganicPostDto>.Failure(workspaceResult.Error)),
                _ => BadRequest(Result<OrganicPostDto>.Failure(workspaceResult.Error))
            };
        }

        var result = await _sender.Send(commandFactory(workspaceId), cancellationToken);
        return MapSocialConnectionResult(result);
    }

    private ActionResult<Result<T>> MapSocialConnectionResult<T>(Result<T> result)
    {
        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.Unauthorized => Unauthorized(result),
                ErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, result),
                ErrorType.NotFound => NotFound(result),
                ErrorType.Validation => UnprocessableEntity(result),
                ErrorType.Conflict => Conflict(result),
                _ => BadRequest(result)
            };
        }

        return Ok(result);
    }

    private ActionResult<Result> MapSocialVoidResult(Result result)
    {
        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.Unauthorized => Unauthorized(result),
                ErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, result),
                ErrorType.NotFound => NotFound(result),
                ErrorType.Validation => UnprocessableEntity(result),
                ErrorType.Conflict => Conflict(result),
                _ => BadRequest(result)
            };
        }

        return Ok(result);
    }

    private ActionResult<Result<T>> MapSocialMediaResult<T>(Result<T> result)
    {
        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.Unauthorized => Unauthorized(result),
                ErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, result),
                ErrorType.NotFound => NotFound(result),
                ErrorType.Validation => UnprocessableEntity(result),
                _ => BadRequest(result)
            };
        }

        return StatusCode(StatusCodes.Status201Created, result);
    }
}

/// <summary>
/// Corpo da requisição para iniciar autorização OAuth orgânica.
/// </summary>
/// <param name="WorkspaceId">Workspace que receberá a conexão.</param>
/// <param name="Platform">Rede orgânica alvo.</param>
/// <param name="RedirectUri">URI de retorno registrada no app da rede.</param>
public sealed record StartOrganicAuthorizationRequest(
    Guid WorkspaceId,
    OrganicPlatform Platform,
    string RedirectUri);

/// <summary>
/// Corpo da requisição de callback OAuth orgânico.
/// </summary>
/// <param name="WorkspaceId">Workspace alvo da conexão.</param>
/// <param name="Code">Código de autorização.</param>
/// <param name="State">State anti-CSRF emitido no authorize.</param>
/// <param name="RedirectUri">URI de retorno utilizada no fluxo.</param>
/// <param name="ProviderError">Erro retornado pelo provedor, se houver.</param>
public sealed record CompleteOrganicAuthorizationRequest(
    Guid WorkspaceId,
    string Code,
    string State,
    string RedirectUri,
    string? ProviderError = null);

/// <summary>
/// Corpo da requisição para upsert de Brand Voice.
/// </summary>
/// <param name="Tone">Tom de voz.</param>
/// <param name="Persona">Persona da marca.</param>
/// <param name="MandatoryLinks">Links absolutos obrigatórios.</param>
/// <param name="ForbiddenTerms">Termos proibidos.</param>
/// <param name="EmojiPolicy">Política de emojis.</param>
/// <param name="HashtagLimit">Teto de hashtags no perfil.</param>
/// <param name="AuthorityWeight">Peso do pilar Authority.</param>
/// <param name="SaleWeight">Peso do pilar Sale.</param>
/// <param name="ConnectionWeight">Peso do pilar Connection.</param>
/// <param name="SocialProofWeight">Peso do pilar SocialProof.</param>
/// <param name="LanguageCode">Código de idioma.</param>
public sealed record UpsertBrandVoiceRequest(
    string Tone,
    string Persona,
    IReadOnlyList<string> MandatoryLinks,
    IReadOnlyList<string> ForbiddenTerms,
    EmojiPolicy EmojiPolicy,
    int HashtagLimit,
    int AuthorityWeight,
    int SaleWeight,
    int ConnectionWeight,
    int SocialProofWeight,
    string LanguageCode = "pt-BR");

/// <summary>
/// Corpo da requisição de reagendamento de post orgânico.
/// </summary>
/// <param name="ScheduledAtUtc">Novo horário agendado em UTC.</param>
public sealed record ReschedulePostRequest(DateTime ScheduledAtUtc);

/// <summary>
/// Corpo da requisição para materializar um plano editorial.
/// </summary>
/// <param name="WorkspaceId">Workspace proprietário.</param>
/// <param name="RangeStart">Data inicial inclusiva.</param>
/// <param name="RangeEnd">Data final inclusiva.</param>
/// <param name="TimeZoneId">Identificador IANA do fuso do workspace.</param>
/// <param name="PostsPerWeek">Posts por semana (1–7).</param>
/// <param name="Seed">Semente determinística do planner.</param>
public sealed record GenerateEditorialPlanRequest(
    Guid WorkspaceId,
    DateOnly RangeStart,
    DateOnly RangeEnd,
    string TimeZoneId,
    int PostsPerWeek,
    int Seed);
