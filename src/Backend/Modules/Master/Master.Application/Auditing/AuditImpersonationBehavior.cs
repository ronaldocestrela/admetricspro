using BuildingBlocks.Application.Security;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Master.Application.Auditing;

/// <summary>
/// Pipeline Behavior do MediatR que intercepta requisições de comandos e consultas no Catálogo Master.
/// Sempre que uma operação é executada sob Shadow Mode (<see cref="IImpersonationContext.IsImpersonated"/> == true),
/// grava automaticamente um evento na trilha de auditoria global imutável com a tag 'performed_by_superadmin'.
/// </summary>
/// <typeparam name="TRequest">Tipo da requisição de comando ou consulta.</typeparam>
/// <typeparam name="TResponse">Tipo do retorno.</typeparam>
public sealed class AuditImpersonationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IImpersonationContext _impersonationContext;
    private readonly IMasterAuditService _auditService;
    private readonly ILogger<AuditImpersonationBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="AuditImpersonationBehavior{TRequest, TResponse}"/>.
    /// </summary>
    /// <param name="impersonationContext">Acesso ao contexto de impersonação.</param>
    /// <param name="auditService">Serviço de auditoria master.</param>
    /// <param name="logger">Instância de logger.</param>
    public AuditImpersonationBehavior(
        IImpersonationContext impersonationContext,
        IMasterAuditService auditService,
        ILogger<AuditImpersonationBehavior<TRequest, TResponse>> logger)
    {
        _impersonationContext = impersonationContext;
        _auditService = auditService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next(cancellationToken);

        if (_impersonationContext.IsImpersonated)
        {
            try
            {
                var requestName = typeof(TRequest).Name;
                await _auditService.RecordAsync(
                    action: $"Command.{requestName}",
                    resource: requestName.Replace("Command", string.Empty).Replace("Query", string.Empty),
                    resourceId: _impersonationContext.TargetTenantId?.ToString(),
                    details: $"Operação executada por SuperAdmin {_impersonationContext.OriginalSuperAdminId} sob o chamado {_impersonationContext.SupportTicketId}",
                    tenantId: _impersonationContext.TargetTenantId,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha não bloqueante ao registrar auditoria automática de impersonation para a requisição {RequestName}", typeof(TRequest).Name);
            }
        }

        return response;
    }
}
