using Analytics.Application.Reports.Commands.CreateReportSchedule;
using Analytics.Application.Reports.DTOs;
using Analytics.Domain.Reports;
using BuildingBlocks.Domain.Reports;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace UnitTests.Backend.Reports;

/// <summary>
/// Testes unitários para o manipulador <see cref="CreateReportScheduleCommandHandler"/>.
/// </summary>
public sealed class CreateReportScheduleCommandHandlerTests
{
    private readonly IReportRepository _repository = Substitute.For<IReportRepository>();
    private readonly CreateReportScheduleCommandHandler _handler;

    /// <summary>
    /// Inicializa a suíte de testes.
    /// </summary>
    public CreateReportScheduleCommandHandlerTests()
    {
        _handler = new CreateReportScheduleCommandHandler(_repository);
    }

    /// <summary>
    /// Valida que a criação de agendamento persiste no repositório e retorna o Id gerado.
    /// </summary>
    [Fact]
    public async Task Handle_WithValidCommand_ShouldPersistScheduleAndReturnId()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var recipients = new List<ReportRecipientDto>
        {
            new("Cliente Gestor", ReportDeliveryChannel.Email, "gestor@cliente.com")
        };

        var command = new CreateReportScheduleCommand(
            workspaceId,
            "Relatório Semanal Automático",
            ReportFrequency.Weekly,
            DayOfWeek.Tuesday,
            null,
            new TimeSpan(8, 30, 0),
            ReportDateRangeType.Last7Days,
            ReportOutputFormat.Both,
            ReportDeliveryChannel.Email,
            "Desempenho de Campanhas",
            "Segue análise semanal.",
            true, true, true, true,
            recipients);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        await _repository.Received(1).AddScheduleAsync(
            Arg.Is<ReportSchedule>(s => s.WorkspaceId == workspaceId && s.Name == "Relatório Semanal Automático"),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que destinatário com e-mail inválido interrompe a criação e retorna falha.
    /// </summary>
    [Fact]
    public async Task Handle_WithInvalidRecipient_ShouldFailValidation()
    {
        // Arrange
        var command = new CreateReportScheduleCommand(
            Guid.NewGuid(),
            "Relatório Teste",
            ReportFrequency.Daily,
            null, null,
            new TimeSpan(9, 0, 0),
            ReportDateRangeType.Last7Days,
            ReportOutputFormat.Pdf,
            ReportDeliveryChannel.Email,
            null, null, false, false, false, false,
            new List<ReportRecipientDto> { new("Cliente", ReportDeliveryChannel.Email, "email-sem-arroba") });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ReportRecipient.InvalidEmail");
        await _repository.DidNotReceive().AddScheduleAsync(Arg.Any<ReportSchedule>(), Arg.Any<CancellationToken>());
    }
}
