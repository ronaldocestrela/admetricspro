using System.Text.Json;
using BuildingBlocks.Domain.Reports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Configuração de mapeamento EF Core para a entidade <see cref="ReportSchedule"/>.
/// </summary>
public sealed class ReportScheduleEntityTypeConfiguration : IEntityTypeConfiguration<ReportSchedule>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ReportSchedule> builder)
    {
        builder.ToTable("ReportSchedules");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.WorkspaceId)
            .IsRequired();

        builder.HasIndex(s => s.WorkspaceId);
        builder.HasIndex(s => new { s.IsActive, s.NextExecutionAtUtc });

        builder.Property(s => s.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.Frequency)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(s => s.DateRangeType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(s => s.OutputFormat)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(s => s.DeliveryChannels)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(s => s.ScheduledTimeUtc)
            .IsRequired();

        builder.Property(s => s.CustomTitle)
            .HasMaxLength(300);

        builder.Property(s => s.CustomNotes)
            .HasMaxLength(4000);

        builder.Property(s => s.IsActive)
            .IsRequired();

        builder.Property(s => s.CreatedAtUtc)
            .IsRequired();

        // Mapeia a lista de destinatários como coluna JSON
        builder.Property(s => s.Recipients)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<List<ReportRecipient>>(v, JsonOptions) ?? new List<ReportRecipient>())
            .HasColumnName("RecipientsJson");
    }
}

/// <summary>
/// Configuração de mapeamento EF Core para a entidade <see cref="GeneratedReport"/>.
/// </summary>
public sealed class GeneratedReportEntityTypeConfiguration : IEntityTypeConfiguration<GeneratedReport>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<GeneratedReport> builder)
    {
        builder.ToTable("GeneratedReports");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.WorkspaceId)
            .IsRequired();

        builder.HasIndex(r => r.WorkspaceId);
        builder.HasIndex(r => r.GeneratedAtUtc);
        builder.HasIndex(r => r.ShareToken).IsUnique();

        builder.Property(r => r.Title)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(r => r.ShareToken)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(r => r.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(r => r.DateRangeStartUtc)
            .IsRequired();

        builder.Property(r => r.DateRangeEndUtc)
            .IsRequired();

        builder.Property(r => r.GeneratedAtUtc)
            .IsRequired();

        builder.Property(r => r.PdfContent);

        builder.Property(r => r.ReportDataPayloadJson)
            .IsRequired();
    }
}

/// <summary>
/// Configuração de mapeamento EF Core para a entidade <see cref="ReportDispatchLog"/>.
/// </summary>
public sealed class ReportDispatchLogEntityTypeConfiguration : IEntityTypeConfiguration<ReportDispatchLog>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ReportDispatchLog> builder)
    {
        builder.ToTable("ReportDispatchLogs");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.GeneratedReportId)
            .IsRequired();

        builder.HasIndex(l => l.GeneratedReportId);
        builder.HasIndex(l => l.DispatchedAtUtc);

        builder.Property(l => l.Channel)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(l => l.Recipient)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(l => l.DispatchedAtUtc)
            .IsRequired();

        builder.Property(l => l.IsSuccess)
            .IsRequired();

        builder.Property(l => l.ErrorMessage)
            .HasMaxLength(2000);
    }
}
