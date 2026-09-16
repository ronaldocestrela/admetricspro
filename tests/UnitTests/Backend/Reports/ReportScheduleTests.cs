using BuildingBlocks.Domain.Reports;
using FluentAssertions;
using Xunit;

namespace UnitTests.Backend.Reports;

/// <summary>
/// Testes unitários para a entidade <see cref="ReportSchedule"/> e objeto de valor <see cref="ReportRecipient"/>.
/// </summary>
public sealed class ReportScheduleTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();

    /// <summary>
    /// Valida a criação de agendamento diário com cálculo da próxima execução e persistência de destinatários.
    /// </summary>
    [Fact]
    public void Create_WithValidDailySchedule_ShouldSucceedAndCalculateNextExecution()
    {
        // Arrange
        var id = Guid.NewGuid();
        var scheduledTime = new TimeSpan(9, 0, 0); // 09:00 UTC
        var now = new DateTime(2026, 9, 16, 8, 0, 0, DateTimeKind.Utc); // 08:00 UTC

        var recipients = new List<ReportRecipient>
        {
            ReportRecipient.Create("Diretor Alfa", ReportDeliveryChannel.Email, "diretor@agenciaalfa.com.br").Value,
            ReportRecipient.Create("Gestor Alfa", ReportDeliveryChannel.WhatsApp, "+5511988887777").Value
        };

        // Act
        var result = ReportSchedule.Create(
            id,
            _workspaceId,
            "Relatório Diário Executivo",
            ReportFrequency.Daily,
            dayOfWeek: null,
            dayOfMonth: null,
            scheduledTime,
            ReportDateRangeType.Last7Days,
            ReportOutputFormat.Both,
            ReportDeliveryChannel.Both,
            customTitle: "Desempenho Diário",
            customNotes: "Acompanhamento de campanhas ativas.",
            includeCopilotInsights: true,
            includeTopCreatives: true,
            includeChannelBreakdown: true,
            includePacingSummary: true,
            recipients,
            now);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var schedule = result.Value;
        schedule.Id.Should().Be(id);
        schedule.WorkspaceId.Should().Be(_workspaceId);
        schedule.Name.Should().Be("Relatório Diário Executivo");
        schedule.Frequency.Should().Be(ReportFrequency.Daily);
        schedule.IsActive.Should().BeTrue();
        schedule.Recipients.Should().HaveCount(2);

        // Como now é 08:00 e o horário é 09:00, o próximo disparo é hoje às 09:00
        schedule.NextExecutionAtUtc.Should().Be(new DateTime(2026, 9, 16, 9, 0, 0, DateTimeKind.Utc));
    }

    /// <summary>
    /// Valida que se o horário agendado já passou no dia de referência, o próximo disparo avança para o dia seguinte.
    /// </summary>
    [Fact]
    public void Create_WhenCurrentTimeIsPastScheduledTime_ShouldScheduleForNextDay()
    {
        // Arrange
        var id = Guid.NewGuid();
        var scheduledTime = new TimeSpan(9, 0, 0); // 09:00 UTC
        var now = new DateTime(2026, 9, 16, 10, 0, 0, DateTimeKind.Utc); // 10:00 UTC (já passou)

        // Act
        var result = ReportSchedule.Create(
            id,
            _workspaceId,
            "Relatório Diário",
            ReportFrequency.Daily,
            dayOfWeek: null,
            dayOfMonth: null,
            scheduledTime,
            ReportDateRangeType.Last7Days,
            ReportOutputFormat.Both,
            ReportDeliveryChannel.Email,
            customTitle: null,
            customNotes: null,
            includeCopilotInsights: false,
            includeTopCreatives: false,
            includeChannelBreakdown: false,
            includePacingSummary: false,
            recipients: null,
            now);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Próximo disparo deve ser no dia seguinte (17/09 às 09:00)
        result.Value.NextExecutionAtUtc.Should().Be(new DateTime(2026, 9, 17, 9, 0, 0, DateTimeKind.Utc));
    }

    /// <summary>
    /// Valida falha na criação de agendamento semanal sem o dia da semana informado.
    /// </summary>
    [Fact]
    public void Create_WithWeeklyScheduleMissingDayOfWeek_ShouldFail()
    {
        // Act
        var result = ReportSchedule.Create(
            Guid.NewGuid(),
            _workspaceId,
            "Relatório Semanal",
            ReportFrequency.Weekly,
            dayOfWeek: null, // Obrigatório para semanal
            dayOfMonth: null,
            new TimeSpan(8, 0, 0),
            ReportDateRangeType.Last7Days,
            ReportOutputFormat.Pdf,
            ReportDeliveryChannel.Email,
            null, null, false, false, false, false, null, DateTime.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ReportSchedule.WeeklyMissingDay");
    }

    /// <summary>
    /// Valida falha na criação de agendamento mensal com dia do mês fora do intervalo permitido.
    /// </summary>
    [Fact]
    public void Create_WithMonthlyScheduleInvalidDay_ShouldFail()
    {
        // Act
        var result = ReportSchedule.Create(
            Guid.NewGuid(),
            _workspaceId,
            "Relatório Mensal",
            ReportFrequency.Monthly,
            dayOfWeek: null,
            dayOfMonth: 32, // Inválido
            new TimeSpan(8, 0, 0),
            ReportDateRangeType.PreviousMonth,
            ReportOutputFormat.Pdf,
            ReportDeliveryChannel.Email,
            null, null, false, false, false, false, null, DateTime.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ReportSchedule.MonthlyInvalidDay");
    }

    /// <summary>
    /// Valida que a gravação de execução atualiza a data e calcula a próxima ocorrência semanal.
    /// </summary>
    [Fact]
    public void RecordExecution_ShouldUpdateLastExecutedAndCalculateNextOccurrence()
    {
        // Arrange
        var schedule = ReportSchedule.Create(
            Guid.NewGuid(),
            _workspaceId,
            "Relatório Semanal",
            ReportFrequency.Weekly,
            DayOfWeek.Friday,
            dayOfMonth: null,
            new TimeSpan(10, 0, 0),
            ReportDateRangeType.Last7Days,
            ReportOutputFormat.Both,
            ReportDeliveryChannel.Email,
            null, null, false, false, false, false, null,
            new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc)).Value; // Segunda-feira

        var executionTime = new DateTime(2026, 9, 18, 10, 0, 0, DateTimeKind.Utc); // Sexta-feira

        // Act
        schedule.RecordExecution(executionTime);

        // Assert
        schedule.LastExecutedAtUtc.Should().Be(executionTime);
        // Próxima sexta-feira: 25/09 às 10:00
        schedule.NextExecutionAtUtc.Should().Be(new DateTime(2026, 9, 25, 10, 0, 0, DateTimeKind.Utc));
    }

    /// <summary>
    /// Valida as regras de validação de e-mail e telefone de ReportRecipient.
    /// </summary>
    [Fact]
    public void ReportRecipient_Validation_ShouldEnforceValidEmailAndPhone()
    {
        // E-mail inválido
        var invalidEmailResult = ReportRecipient.Create("Fulano", ReportDeliveryChannel.Email, "email-invalido");
        invalidEmailResult.IsFailure.Should().BeTrue();
        invalidEmailResult.Error.Code.Should().Be("ReportRecipient.InvalidEmail");

        // Telefone inválido (menos de 10 dígitos)
        var invalidPhoneResult = ReportRecipient.Create("Fulano", ReportDeliveryChannel.WhatsApp, "123");
        invalidPhoneResult.IsFailure.Should().BeTrue();
        invalidPhoneResult.Error.Code.Should().Be("ReportRecipient.InvalidPhone");

        // Casos válidos
        var validEmailResult = ReportRecipient.Create("Cliente", ReportDeliveryChannel.Email, "cliente@empresa.com");
        validEmailResult.IsSuccess.Should().BeTrue();

        var validPhoneResult = ReportRecipient.Create("Cliente", ReportDeliveryChannel.WhatsApp, "+55 11 99999-8888");
        validPhoneResult.IsSuccess.Should().BeTrue();
    }
}
