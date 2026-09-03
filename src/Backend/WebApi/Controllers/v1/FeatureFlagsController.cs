using BuildingBlocks.Domain.Primitives;
using Master.Application.FeatureFlags.Commands.ActivateKillSwitch;
using Master.Application.FeatureFlags.Commands.CreateFeatureFlag;
using Master.Application.FeatureFlags.Commands.DeactivateKillSwitch;
using Master.Application.FeatureFlags.Commands.UpdateFeatureFlag;
using Master.Application.FeatureFlags.DTOs;
using Master.Application.FeatureFlags.Queries.EvaluateFeatureFlag;
using Master.Application.FeatureFlags.Queries.GetFeatureFlagByKey;
using Master.Application.FeatureFlags.Queries.GetFeatureFlags;
using Master.Application.FeatureFlags.Services;
using Master.Domain.Integrations;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;

namespace WebApi.Controllers.v1;

/// <summary>
/// Controlador responsável pela governança de Feature Flags dinâmicas e Disjuntores Operacionais (Kill Switches) no Catálogo Master.
/// Permite o congelamento em tempo real do motor de automação cross-network e lançamentos progressivos.
/// </summary>
[ApiController]
[Route("api/v1/admin/feature-flags")]
public sealed class FeatureFlagsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IFeatureFlagService _featureFlagService;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="FeatureFlagsController"/>.
    /// </summary>
    /// <param name="sender">Mediador de comandos e consultas.</param>
    /// <param name="featureFlagService">Serviço de alta performance para avaliação de flags.</param>
    public FeatureFlagsController(ISender sender, IFeatureFlagService featureFlagService)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _featureFlagService = featureFlagService ?? throw new ArgumentNullException(nameof(featureFlagService));
    }

    /// <summary>
    /// Lista todas as feature flags e disjuntores operacionais cadastrados no sistema.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Coleção de flags e kill switches com metadados de rollout e estado.</returns>
    [HttpGet]
    [EndpointSummary("Obtém o catálogo completo de feature flags e kill switches")]
    [ProducesResponseType(typeof(Result<IReadOnlyList<FeatureFlagDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Result<IReadOnlyList<FeatureFlagDto>>>> GetAll(CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetFeatureFlagsQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Obtém detalhes de uma feature flag ou kill switch pela sua chave identificadora.
    /// </summary>
    /// <param name="key">Chave única da flag (ex: 'killswitch.automation.global').</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Dados detalhados da flag ou erro 404 se inexistente.</returns>
    [HttpGet("{key}")]
    [EndpointSummary("Obtém uma feature flag pela chave identificadora")]
    [ProducesResponseType(typeof(Result<FeatureFlagDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<FeatureFlagDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result<FeatureFlagDto>>> GetByKey(string key, CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetFeatureFlagByKeyQuery(key), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.NotFound => NotFound(result),
                _ => BadRequest(result)
            };
        }

        return Ok(result);
    }

    /// <summary>
    /// Cadastra uma nova feature flag no catálogo do MasterDb.
    /// </summary>
    /// <param name="request">Configurações da nova flag.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>ID da flag criada ou erro de validação.</returns>
    [HttpPost]
    [EndpointSummary("Cadastra uma nova feature flag ou kill switch")]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Result<Guid>>> Create(
        [FromBody] CreateFeatureFlagApiRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateFeatureFlagCommand(
            request.Key,
            request.Name,
            request.Description,
            request.IsEnabled,
            request.IsKillSwitch,
            request.TargetingType,
            request.RolloutPercentage,
            request.TargetTenantIds,
            User.Identity?.Name ?? "backoffice-admin");

        var result = await _sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.Conflict => Conflict(result),
                ErrorType.Validation => UnprocessableEntity(result),
                _ => BadRequest(result)
            };
        }

        return CreatedAtAction(nameof(GetByKey), new { key = request.Key.ToLowerInvariant() }, result);
    }

    /// <summary>
    /// Atualiza os parâmetros operacionais, percentual de rollout ou allowlist de uma flag existente.
    /// </summary>
    /// <param name="id">Identificador único da flag.</param>
    /// <param name="request">Novos parâmetros da flag.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    [HttpPut("{id:guid}")]
    [EndpointSummary("Atualiza os parâmetros de rollout e status da flag")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Result>> Update(
        Guid id,
        [FromBody] UpdateFeatureFlagApiRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateFeatureFlagCommand(
            id,
            request.IsEnabled,
            request.TargetingType,
            request.RolloutPercentage,
            request.TargetTenantIds,
            User.Identity?.Name ?? "backoffice-admin");

        var result = await _sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.NotFound => NotFound(result),
                ErrorType.Validation => UnprocessableEntity(result),
                _ => BadRequest(result)
            };
        }

        return Ok(result);
    }

    /// <summary>
    /// Aciona imediatamente um Kill Switch operacional de emergência, congelando o subsistema associado.
    /// </summary>
    /// <param name="key">Chave do Kill Switch.</param>
    /// <param name="request">Motivo/justificativa técnica obrigatória.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da ativação.</returns>
    [HttpPost("{key}/kill-switch/activate")]
    [EndpointSummary("Aciona o Kill Switch para congelamento emergencial")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Result>> ActivateKillSwitch(
        string key,
        [FromBody] ActivateKillSwitchApiRequest request,
        CancellationToken cancellationToken = default)
    {
        var triggeredBy = request.TriggeredBy ?? User.Identity?.Name ?? "backoffice-admin";
        var command = new ActivateKillSwitchCommand(key, request.Reason, triggeredBy);

        var result = await _sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.NotFound => NotFound(result),
                ErrorType.Validation => UnprocessableEntity(result),
                _ => BadRequest(result)
            };
        }

        return Ok(result);
    }

    /// <summary>
    /// Desativa um Kill Switch operacional, restabelecendo o fluxo normal de execuções do subsistema.
    /// </summary>
    /// <param name="key">Chave do Kill Switch.</param>
    /// <param name="request">Justificativa operacional da restauração.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da desativação.</returns>
    [HttpPost("{key}/kill-switch/deactivate")]
    [EndpointSummary("Desativa o Kill Switch e restaura o funcionamento normal")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Result>> DeactivateKillSwitch(
        string key,
        [FromBody] DeactivateKillSwitchApiRequest request,
        CancellationToken cancellationToken = default)
    {
        var triggeredBy = request.TriggeredBy ?? User.Identity?.Name ?? "backoffice-admin";
        var command = new DeactivateKillSwitchCommand(key, request.Reason, triggeredBy);

        var result = await _sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.NotFound => NotFound(result),
                ErrorType.Validation => UnprocessableEntity(result),
                _ => BadRequest(result)
            };
        }

        return Ok(result);
    }

    /// <summary>
    /// Avalia se a feature flag está habilitada para o contexto opcional de inquilino informado.
    /// </summary>
    /// <param name="key">Chave da feature flag.</param>
    /// <param name="tenantId">Identificador do inquilino (opcional).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Booleano indicando se o recurso está ativo no contexto.</returns>
    [HttpGet("{key}/evaluate")]
    [EndpointSummary("Avalia se a feature flag está habilitada para o tenant")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result<bool>>> Evaluate(
        string key,
        [FromQuery] Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new EvaluateFeatureFlagQuery(key, tenantId), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.NotFound => NotFound(result),
                _ => BadRequest(result)
            };
        }

        return Ok(result);
    }

    /// <summary>
    /// Consulta rápida para verificar se o motor de automação cross-network está congelado por um Kill Switch ativo.
    /// </summary>
    /// <param name="platform">Plataforma opcional (Meta, Google, TikTok, Bing) ou nulo para verificação geral.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Status consolidado de congelamento operacional.</returns>
    [HttpGet("automation-status")]
    [EndpointSummary("Verifica se o motor de automações está congelado")]
    [ProducesResponseType(typeof(Result<AutomationEngineStatusDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Result<AutomationEngineStatusDto>>> GetAutomationStatus(
        [FromQuery] AdPlatform? platform = null,
        CancellationToken cancellationToken = default)
    {
        var isFrozen = await _featureFlagService.IsAutomationFrozenAsync(platform, cancellationToken);
        var activeKey = isFrozen ? (platform.HasValue ? $"killswitch.automation.{platform.Value.ToString().ToLowerInvariant()}" : "killswitch.automation.global") : null;

        var status = new AutomationEngineStatusDto(
            IsFrozen: isFrozen,
            Platform: platform,
            ActiveKillSwitchKey: activeKey,
            CheckedAtUtc: DateTime.UtcNow);

        return Ok(Result<AutomationEngineStatusDto>.Success(status));
    }
}
