using Automations.Application.Rules.Commands.CreateRule;
using Automations.Application.Rules.Commands.DeleteRule;
using Automations.Application.Rules.Commands.EvaluateRule;
using Automations.Application.Rules.Commands.ToggleRuleState;
using Automations.Application.Rules.Commands.UpdateRule;
using Automations.Application.Rules.DTOs;
using Automations.Application.Rules.Queries.GetRuleById;
using Automations.Application.Rules.Queries.ListRulesByWorkspace;
using BuildingBlocks.Domain.Automations;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using WebApi.Controllers.v1;
using Xunit;

namespace UnitTests.Backend.Automations;

/// <summary>
/// Testes unitários para o controlador AutomationsController (Web API RESTful / OpenAPI + Scalar).
/// </summary>
public sealed class AutomationsControllerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly AutomationsController _controller;
    private readonly Guid _workspaceId = Guid.NewGuid();

    /// <summary>
    /// Inicializa o controlador com mock do ISender.
    /// </summary>
    public AutomationsControllerTests()
    {
        _controller = new AutomationsController(_sender);
    }

    /// <summary>
    /// Valida que POST /api/v1/automations/rules retorna 201 Created quando o comando é bem-sucedido.
    /// </summary>
    [Fact]
    public async Task CreateRule_ShouldReturnCreated_WhenSuccessful()
    {
        var ruleId = Guid.NewGuid();
        _sender.Send(Arg.Any<CreateRuleCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Success(ruleId));

        var condition = new MetricPredicate("TikTokAds", RuleScope.Campaign, MetricType.Cpa, ComparisonOperator.GreaterThan, 50m, 48);
        var group = new RuleConditionGroup(LogicalOperator.And, new[] { condition });
        var action = new RuleAction(RuleActionType.PauseCampaign);

        var request = new WebApi.Models.CreateRuleApiRequest
        {
            WorkspaceId = _workspaceId,
            Name = "Regra TikTok",
            Description = "Descrição",
            ConditionTree = group,
            Actions = new List<RuleAction> { action },
            IsEnabled = true
        };

        var response = await _controller.CreateRule(request, CancellationToken.None);

        var createdResult = response.Result as CreatedAtActionResult;
        createdResult.Should().NotBeNull();
        createdResult!.StatusCode.Should().Be(201);
    }

    /// <summary>
    /// Valida que GET /api/v1/automations/rules retorna 200 OK com lista de regras.
    /// </summary>
    [Fact]
    public async Task ListRules_ShouldReturnOk_WithRulesList()
    {
        var rules = new List<AutomationRuleDto>();
        _sender.Send(Arg.Any<ListRulesByWorkspaceQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<AutomationRuleDto>>.Success(rules));

        var response = await _controller.ListRules(_workspaceId, CancellationToken.None);

        var okResult = response.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    /// <summary>
    /// Valida que GET /api/v1/automations/rules/{id} retorna 404 NotFound quando a regra não existe.
    /// </summary>
    [Fact]
    public async Task GetRuleById_ShouldReturnNotFound_WhenRuleDoesNotExist()
    {
        _sender.Send(Arg.Any<GetRuleByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<AutomationRuleDto>.Failure(Error.NotFound("AutomationRule.NotFound", "Não localizada.")));

        var response = await _controller.GetRuleById(Guid.NewGuid(), _workspaceId, CancellationToken.None);

        var notFoundResult = response.Result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult!.StatusCode.Should().Be(404);
    }

    /// <summary>
    /// Valida que POST /api/v1/automations/rules/{id}/evaluate retorna 200 OK com o relatório de execução.
    /// </summary>
    [Fact]
    public async Task EvaluateRule_ShouldReturnOk_WithReport()
    {
        var ruleId = Guid.NewGuid();
        var report = new RuleEvaluationReportDto(ruleId, "Regra", true, 1, 1, new[] { "Sucesso" });

        _sender.Send(Arg.Any<EvaluateRuleCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<RuleEvaluationReportDto>.Success(report));

        var request = new WebApi.Models.EvaluateRuleApiRequest { WorkspaceId = _workspaceId };
        var response = await _controller.EvaluateRule(ruleId, request, CancellationToken.None);

        var okResult = response.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }
}
