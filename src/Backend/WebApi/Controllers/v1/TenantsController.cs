using BuildingBlocks.Domain.Primitives;
using Master.Application.Tenants.Commands.ImpersonateTenant;
using Master.Domain.Tenants;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;

namespace WebApi.Controllers.v1;

/// <summary>
/// Controlador responsável pela gestão de tenants, workspaces e operações de suporte técnico seguro (Shadow Mode).
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public sealed class TenantsController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TenantsController"/>.
    /// </summary>
    /// <param name="sender">Mediador in-memory de comandos e consultas.</param>
    public TenantsController(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    /// <summary>
    /// Emite um token JWT de impersonação contextual seguro com claims de auditoria para suporte técnico.
    /// </summary>
    /// <param name="tenantId">Identificador do tenant a ser acessado em Shadow Mode.</param>
    /// <param name="request">Parâmetros de justificativa e identificação de suporte.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    /// <returns>Resultado contendo o token de acesso e metadados da sessão.</returns>
    [HttpPost("{tenantId:guid}/impersonate")]
    [EndpointSummary("Emite token JWT contextual de impersonação (Shadow Mode) para suporte técnico auditado")]
    [ProducesResponseType(typeof(Result<ImpersonateTenantResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<ImpersonateTenantResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<ImpersonateTenantResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result<ImpersonateTenantResponse>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Result<ImpersonateTenantResponse>>> ImpersonateTenant(
        [FromRoute] Guid tenantId,
        [FromBody] ImpersonateTenantApiRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequest(Result<ImpersonateTenantResponse>.Failure(
                Error.Validation("Request.Null", "O corpo da requisição não pode ser nulo.")));
        }

        var command = new ImpersonateTenantCommand(
            new TenantId(tenantId),
            request.SuperAdminId,
            request.SupportTicketId,
            request.Reason,
            request.DurationMinutes);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.NotFound => NotFound(Result<ImpersonateTenantResponse>.Failure(result.Error)),
                ErrorType.Validation => UnprocessableEntity(Result<ImpersonateTenantResponse>.Failure(result.Error)),
                _ => BadRequest(Result<ImpersonateTenantResponse>.Failure(result.Error))
            };
        }

        return Ok(Result<ImpersonateTenantResponse>.Success(result.Value));
    }

    /// <summary>
    /// Encerra imediatamente uma sessão de Shadow Mode ativa, revogando seu acesso e registrando na trilha de auditoria global.
    /// </summary>
    /// <param name="tenantId">Identificador do tenant sob impersonação.</param>
    /// <param name="sessionId">Identificador único da sessão a ser revogada.</param>
    /// <param name="request">Carga opcional contendo justificativa de encerramento.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Resultado da operação.</returns>
    [HttpPost("{tenantId:guid}/impersonate/{sessionId:guid}/terminate")]
    [EndpointSummary("Encerra imediatamente uma sessão de impersonation (Shadow Mode) ativa")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Result>> TerminateImpersonation(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid sessionId,
        [FromBody] TerminateImpersonationApiRequest? request,
        CancellationToken cancellationToken)
    {
        var command = new Master.Application.Tenants.Commands.TerminateImpersonationSession.TerminateImpersonationSessionCommand(
            tenantId,
            sessionId,
            request?.Reason);

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
    /// Verifica em tempo real se um subdomínio desejado está disponível para alocação de novo tenant.
    /// </summary>
    /// <param name="subdomain">Subdomínio pretendido pelo novo assinante.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    /// <returns>Status de disponibilidade, motivo em caso de recusa e eventuais sugestões.</returns>
    [HttpGet("check-subdomain")]
    [EndpointSummary("Verifica a disponibilidade de um subdomínio para cadastro de novo tenant")]
    [ProducesResponseType(typeof(Result<Master.Application.Tenants.Queries.CheckSubdomainAvailability.SubdomainAvailabilityResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<Master.Application.Tenants.Queries.CheckSubdomainAvailability.SubdomainAvailabilityResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Result<Master.Application.Tenants.Queries.CheckSubdomainAvailability.SubdomainAvailabilityResponse>>> CheckSubdomain(
        [FromQuery] string subdomain,
        CancellationToken cancellationToken)
    {
        var query = new Master.Application.Tenants.Queries.CheckSubdomainAvailability.CheckSubdomainAvailabilityQuery(subdomain);
        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Submete o cadastro completo de um novo tenant, disparando a engine de provisionamento de banco SQL Server dedicado.
    /// </summary>
    /// <param name="request">Dados corporativos, de plano, White-Label e credenciais do gestor.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Resultado com identificador do tenant, subdomínio ativo e URL de acesso ao painel.</returns>
    [HttpPost("onboarding")]
    [EndpointSummary("Registra e provisiona um novo inquilino com banco dedicado e credenciais do administrador")]
    [ProducesResponseType(typeof(Result<Master.Application.Tenants.Commands.RegisterTenantOnboarding.TenantOnboardingResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<Master.Application.Tenants.Commands.RegisterTenantOnboarding.TenantOnboardingResult>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<Master.Application.Tenants.Commands.RegisterTenantOnboarding.TenantOnboardingResult>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(Result<Master.Application.Tenants.Commands.RegisterTenantOnboarding.TenantOnboardingResult>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Result<Master.Application.Tenants.Commands.RegisterTenantOnboarding.TenantOnboardingResult>>> RegisterOnboarding(
        [FromBody] TenantOnboardingApiRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(Result<Master.Application.Tenants.Commands.RegisterTenantOnboarding.TenantOnboardingResult>.Failure(
                Error.Validation("Request.Null", "O corpo da requisição não pode ser nulo.")));
        }

        var command = new Master.Application.Tenants.Commands.RegisterTenantOnboarding.RegisterTenantOnboardingCommand(
            request.CompanyName,
            request.Cnpj,
            request.Subdomain,
            request.Segment,
            request.MonthlyAdSpendRange,
            request.Tier,
            request.BillingCycle,
            request.AdminFullName,
            request.AdminEmail,
            request.AdminPhone,
            request.AdminPassword,
            request.CustomDomain,
            request.PrimaryColor,
            request.SecondaryColor);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.Conflict => Conflict(Result<Master.Application.Tenants.Commands.RegisterTenantOnboarding.TenantOnboardingResult>.Failure(result.Error)),
                ErrorType.Validation => UnprocessableEntity(Result<Master.Application.Tenants.Commands.RegisterTenantOnboarding.TenantOnboardingResult>.Failure(result.Error)),
                _ => BadRequest(Result<Master.Application.Tenants.Commands.RegisterTenantOnboarding.TenantOnboardingResult>.Failure(result.Error))
            };
        }

        return Ok(Result<Master.Application.Tenants.Commands.RegisterTenantOnboarding.TenantOnboardingResult>.Success(result.Value));
    }

    /// <summary>
    /// Lista todos os inquilinos registrados no catálogo Master para o diretório corporativo.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Coleção com metadados e status de todos os inquilinos.</returns>
    [HttpGet]
    [EndpointSummary("Lista todos os inquilinos cadastrados no catálogo")]
    [ProducesResponseType(typeof(Result<IReadOnlyList<Master.Application.Tenants.Queries.GetTenantDetails.TenantDetailsResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Result<IReadOnlyList<Master.Application.Tenants.Queries.GetTenantDetails.TenantDetailsResponse>>>> GetTenants(
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new Master.Application.Tenants.Queries.GetTenants.GetTenantsQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Obtém a visão 360º detalhada de um inquilino pelo seu identificador único.
    /// </summary>
    /// <param name="tenantId">Identificador único do inquilino.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Dados consolidados do inquilino ou 404 se não encontrado.</returns>
    [HttpGet("{tenantId:guid}")]
    [EndpointSummary("Obtém os detalhes operacionais e de plano de um inquilino")]
    [ProducesResponseType(typeof(Result<Master.Application.Tenants.Queries.GetTenantDetails.TenantDetailsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<Master.Application.Tenants.Queries.GetTenantDetails.TenantDetailsResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result<Master.Application.Tenants.Queries.GetTenantDetails.TenantDetailsResponse>>> GetTenantById(
        [FromRoute] Guid tenantId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new Master.Application.Tenants.Queries.GetTenantDetails.GetTenantDetailsQuery(new TenantId(tenantId)),
            cancellationToken);

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
    /// Suspende temporariamente as operações e acessos de um inquilino.
    /// </summary>
    /// <param name="tenantId">Identificador único do inquilino a ser suspenso.</param>
    /// <param name="request">Justificativa da suspensão.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    [HttpPost("{tenantId:guid}/suspend")]
    [EndpointSummary("Suspende as operações de um inquilino")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result>> SuspendTenant(
        [FromRoute] Guid tenantId,
        [FromBody] SuspendTenantApiRequest request,
        CancellationToken cancellationToken)
    {
        var reason = request?.Reason ?? "Suspensão solicitada pela administração";
        var command = new Master.Application.Tenants.Commands.SuspendTenant.SuspendTenantCommand(new TenantId(tenantId), reason);

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
    /// Reativa um inquilino previamente suspenso, restaurando o acesso regular.
    /// </summary>
    /// <param name="tenantId">Identificador único do inquilino.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    [HttpPost("{tenantId:guid}/reactivate")]
    [EndpointSummary("Reativa um inquilino suspenso")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result>> ReactivateTenant(
        [FromRoute] Guid tenantId,
        CancellationToken cancellationToken)
    {
        var command = new Master.Application.Tenants.Commands.ReactivateTenant.ReactivateTenantCommand(new TenantId(tenantId));

        var result = await _sender.Send(command, cancellationToken);
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
}
