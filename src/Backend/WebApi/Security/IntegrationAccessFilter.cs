using BuildingBlocks.Application.Integrations;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WebApi.Security;

/// <summary>Protege as rotas de integração por identidade assinada, tenant e carteira de workspaces.</summary>
/// <param name="tenants">Contexto do tenant autenticado.</param>
/// <param name="sender">Mediador de contratos e comandos.</param>
public sealed class IntegrationAccessFilter(ITenantContextAccessor tenants, ISender sender) : IAsyncActionFilter
{
    /// <summary>Identifica as rotas de integração e os consumidores de seus dados persistidos.</summary>
    /// <param name="path">Caminho HTTP.</param>
    /// <returns>True para rotas que precisam da barreira contextual de marketing.</returns>
    public static bool Protects(PathString path) => path.StartsWithSegments("/api/v1/integrations") ||
        path.StartsWithSegments("/api/v1/workspaces") || path.StartsWithSegments("/api/v1/analytics/dashboard") ||
        path.StartsWithSegments("/api/v1/analytics/creatives") || path.StartsWithSegments("/api/v1/analytics/copilot") ||
        path.StartsWithSegments("/api/v1/automations");
    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        if (!Protects(http.Request.Path))
        {
            await next();
            return;
        }
        if (http.User.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedObjectResult(Result.Failure(Error.Unauthorized("Auth.Unauthorized", "Autenticação necessária.")));
            return;
        }
        if (!Guid.TryParse(http.User.FindFirst("tenant_id")?.Value, out var tenantId) || tenantId == Guid.Empty ||
            tenants.TenantContext?.TenantId != tenantId)
        {
            context.Result = new ObjectResult(Result.Failure(Error.Forbidden("Auth.TenantMismatch", "O tenant informado não corresponde à sessão autenticada."))) { StatusCode = 403 };
            return;
        }
        // O callback extrai o workspace exclusivamente do state e o valida no handler.
        if (http.Request.Path.Equals(new PathString("/api/v1/integrations/oauth/callback")))
        {
            await next();
            return;
        }
        // A coleção é filtrada pela carteira do usuário autenticado dentro do controller.
        if (http.Request.Method == "GET" && http.Request.Path.Equals(new PathString("/api/v1/workspaces")))
        {
            await next();
            return;
        }
        Guid? explicitWorkspace = null;
        Guid? bodyWorkspace = null;
        foreach (var (key, value) in context.ActionArguments)
        {
            if (string.Equals(key, "workspaceId", StringComparison.OrdinalIgnoreCase) && value is Guid id)
                explicitWorkspace = id;
            else if (string.Equals(key, "id", StringComparison.OrdinalIgnoreCase) && value is Guid workspace && context.Controller is Controllers.v1.WorkspacesController)
                explicitWorkspace = workspace;
            else if (value?.GetType().GetProperty("WorkspaceId")?.GetValue(value) is Guid nested)
                bodyWorkspace = nested;
        }
        if (explicitWorkspace.HasValue && bodyWorkspace.HasValue && explicitWorkspace != bodyWorkspace)
        {
            context.Result = new ObjectResult(Result.Failure(Error.Forbidden("Auth.WorkspaceMismatch", "O workspace do corpo deve corresponder ao workspace solicitado na rota."))) { StatusCode = 403 };
            return;
        }
        var workspaceId = explicitWorkspace ?? bodyWorkspace ?? Guid.Empty;
        if (http.Request.Path.Equals(new PathString("/api/v1/automations/pacing/simulate")))
        { await next(); return; } // Cálculo puro com dados fornecidos, sem consulta de dados do tenant.
        var manage = http.Request.Path.StartsWithSegments("/api/v1/integrations/oauth") || http.Request.Method != "GET";
        var editCampaigns = http.Request.Method != "GET" && (http.Request.Path.StartsWithSegments("/api/v1/integrations/meta/resources") || http.Request.Path.StartsWithSegments("/api/v1/integrations/meta/media") ||
            http.Request.Path.Equals(new PathString("/api/v1/integrations/campaigns/bulk")));
        if (http.Request.Path.Value?.EndsWith("/sync", StringComparison.OrdinalIgnoreCase) == true || editCampaigns) manage = false;
        BuildingBlocks.Domain.Tenants.TenantPermission? permission = null;
        if (http.Request.Method != "GET" && http.Request.Path.StartsWithSegments("/api/v1/automations"))
            permission = BuildingBlocks.Domain.Tenants.TenantPermission.ManageAutomations;
        if (http.Request.Path.StartsWithSegments("/api/v1/automations/pacing"))
            permission = BuildingBlocks.Domain.Tenants.TenantPermission.ViewCampaigns;
        if (http.Request.Method != "GET" && http.Request.Path.StartsWithSegments("/api/v1/analytics/copilot"))
            permission = BuildingBlocks.Domain.Tenants.TenantPermission.EditCampaigns;
        if (context.Controller is Controllers.v1.ReportsController)
            permission = BuildingBlocks.Domain.Tenants.TenantPermission.ExportReports;
        var result = await sender.Send(new ValidateIntegrationWorkspaceQuery(workspaceId, manage, editCampaigns,
            permission, IncludeInactive: context.Controller is Controllers.v1.WorkspacesController), http.RequestAborted);
        if (result.IsFailure || !result.Value)
        {
            var error = result.IsFailure ? result.Error : Error.Forbidden("Auth.WorkspaceForbidden", "Sem permissão para este workspace.");
            context.Result = new ObjectResult(Result.Failure(error))
            { StatusCode = error.Type == ErrorType.Unauthorized ? 401 : error.Type == ErrorType.NotFound ? 404 : 403 };
            return;
        }
        await next();
    }
}
