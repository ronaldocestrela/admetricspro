using System.Security.Claims;
using BuildingBlocks.Application.MultiTenancy;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using NSubstitute;
using WebApi.Security;

namespace UnitTests.Backend.Integrations.OAuth;

/// <summary>Valida a barreira HTTP de isolamento das integrações.</summary>
public sealed class IntegrationAccessFilterTests
{
    /// <summary>O corpo não pode autorizar A enquanto o endpoint executa sobre B na rota.</summary>
    [Fact]
    public async Task Action_WithDifferentRouteAndBodyWorkspace_IsRejectedBeforeAuthorization()
    {
        var tenantId = Guid.NewGuid();
        var http = new DefaultHttpContext(); http.Request.Path = "/api/v1/integrations/meta/resources";
        http.User = new(new ClaimsIdentity([new Claim("tenant_id", tenantId.ToString())], "Tenant"));
        var tenant = Substitute.For<ITenantContext>(); tenant.TenantId.Returns(tenantId);
        var accessor = Substitute.For<ITenantContextAccessor>(); accessor.TenantContext.Returns(tenant);
        var sender = Substitute.For<ISender>();
        var context = new ActionExecutingContext(new ActionContext(http, new RouteData(), new ActionDescriptor()), [],
            new Dictionary<string, object?> { ["workspaceId"] = Guid.NewGuid(), ["request"] = new WorkspaceRequest(Guid.NewGuid()) }, new object());
        await new IntegrationAccessFilter(accessor, sender).OnActionExecutionAsync(context, () => throw new InvalidOperationException("Não deve executar o endpoint."));
        context.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(403);
        sender.ReceivedCalls().Should().BeEmpty();
    }

    private sealed record WorkspaceRequest(Guid WorkspaceId);
    /// <summary>Headers de tenant não podem substituir a identidade assinada.</summary>
    [Fact]
    public async Task Action_WithDifferentTenantClaim_IsForbiddenBeforeMediator()
    {
        var http = new DefaultHttpContext();
        http.Request.Path = "/api/v1/integrations/meta/ad-accounts";
        http.User = new(new ClaimsIdentity([new Claim("tenant_id", Guid.NewGuid().ToString())], "Tenant"));
        var tenant = Substitute.For<ITenantContext>();
        tenant.TenantId.Returns(Guid.NewGuid());
        var accessor = Substitute.For<ITenantContextAccessor>();
        accessor.TenantContext.Returns(tenant);
        var sender = Substitute.For<ISender>();
        var context = new ActionExecutingContext(new ActionContext(http, new RouteData(), new ActionDescriptor()),
            [], new Dictionary<string, object?> { ["workspaceId"] = Guid.NewGuid() }, new object());
        var called = false;
        await new IntegrationAccessFilter(accessor, sender).OnActionExecutionAsync(context, () =>
        {
            called = true;
            return Task.FromResult(new ActionExecutedContext(context, [], new object()));
        });
        context.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(403);
        called.Should().BeFalse();
        sender.ReceivedCalls().Should().BeEmpty();
    }
}
