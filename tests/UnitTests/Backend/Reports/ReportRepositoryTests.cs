using Analytics.Infrastructure.Reports;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Reports;
using BuildingBlocks.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace UnitTests.Backend.Reports;

/// <summary>
/// Testes unitários de persistência e repositório para <see cref="ReportRepository"/> sobre o <see cref="TenantDbContext"/>.
/// </summary>
public sealed class ReportRepositoryTests : IDisposable
{
    private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;
    private readonly TenantDbContext _dbContext;
    private readonly ITenantDbContextAccessor _contextAccessor = Substitute.For<ITenantDbContextAccessor>();
    private readonly ReportRepository _repository;

    /// <summary>
    /// Inicializa a base SQLite em memória do TenantDbContext e o repositório sob teste.
    /// </summary>
    public ReportRepositoryTests()
    {
        _connection = new Microsoft.Data.Sqlite.SqliteConnection("Filename=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new TenantDbContext(options);
        _dbContext.Database.EnsureCreated();

        _contextAccessor.GetDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<TenantDbContext>.Success(_dbContext)));

        _repository = new ReportRepository(_contextAccessor);
    }

    /// <summary>
    /// Limpa o banco em memória ao término dos testes.
    /// </summary>
    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    /// <summary>
    /// Valida que a gravação e recuperação de um agendamento de relatório funciona com todos os campos e destinatários.
    /// </summary>
    [Fact]
    public async Task AddScheduleAsync_And_GetScheduleByIdAsync_ShouldPersistAndRetrieveCorrectly()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var scheduleId = Guid.NewGuid();
        var recipients = new List<ReportRecipient>
        {
            ReportRecipient.Create("Diretor", ReportDeliveryChannel.Email, "diretor@agencia.com").Value
        };

        var schedule = ReportSchedule.Create(
            scheduleId,
            workspaceId,
            "Relatório Semanal Executivo",
            ReportFrequency.Weekly,
            DayOfWeek.Monday,
            null,
            new TimeSpan(8, 0, 0),
            ReportDateRangeType.Last7Days,
            ReportOutputFormat.Both,
            ReportDeliveryChannel.Email,
            "Resumo Semanal",
            "Notas de gestão",
            true, true, true, true,
            recipients,
            DateTime.UtcNow).Value;

        // Act
        await _repository.AddScheduleAsync(schedule);
        var retrieved = await _repository.GetScheduleByIdAsync(scheduleId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(scheduleId);
        retrieved.WorkspaceId.Should().Be(workspaceId);
        retrieved.Name.Should().Be("Relatório Semanal Executivo");
        retrieved.Recipients.Should().HaveCount(1);
        retrieved.Recipients[0].Destination.Should().Be("diretor@agencia.com");
    }

    /// <summary>
    /// Valida que GetPendingSchedulesAsync retorna apenas regras ativas com vencimento até a data limite.
    /// </summary>
    [Fact]
    public async Task GetPendingSchedulesAsync_ShouldReturnOnlyDueActiveSchedules()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var dueSchedule = ReportSchedule.Create(
            Guid.NewGuid(), workspaceId, "Pendente", ReportFrequency.Daily, null, null,
            new TimeSpan(8, 0, 0), ReportDateRangeType.Last7Days, ReportOutputFormat.Pdf, ReportDeliveryChannel.Email,
            null, null, false, false, false, false, null, DateTime.UtcNow.AddDays(-2)).Value;

        var futureSchedule = ReportSchedule.Create(
            Guid.NewGuid(), workspaceId, "Futuro", ReportFrequency.Daily, null, null,
            new TimeSpan(8, 0, 0), ReportDateRangeType.Last7Days, ReportOutputFormat.Pdf, ReportDeliveryChannel.Email,
            null, null, false, false, false, false, null, DateTime.UtcNow.AddDays(2)).Value;

        await _repository.AddScheduleAsync(dueSchedule);
        await _repository.AddScheduleAsync(futureSchedule);

        // Act
        var pending = await _repository.GetPendingSchedulesAsync(DateTime.UtcNow);

        // Assert
        pending.Should().Contain(s => s.Id == dueSchedule.Id);
        pending.Should().NotContain(s => s.Id == futureSchedule.Id);
    }

    /// <summary>
    /// Valida que a gravação de GeneratedReport e busca por ShareToken funcionam perfeitamente.
    /// </summary>
    [Fact]
    public async Task AddGeneratedReportAsync_And_GetByShareTokenAsync_ShouldWork()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var token = "token-unico-1234567890abcdef";

        var report = GeneratedReport.Create(
            reportId,
            workspaceId,
            null,
            "Relatório Mensal",
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(30),
            new byte[] { 1, 2, 3 },
            "{}",
            DateTime.UtcNow,
            customToken: token).Value;

        // Act
        await _repository.AddGeneratedReportAsync(report);
        var retrieved = await _repository.GetReportByShareTokenAsync(token);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(reportId);
        retrieved.ShareToken.Should().Be(token);
        retrieved.PdfContent.Should().NotBeNull();
    }
}
