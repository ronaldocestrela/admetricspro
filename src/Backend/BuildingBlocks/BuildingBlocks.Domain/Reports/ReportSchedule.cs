using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Domain.Reports;

/// <summary>
/// Representa a configuração e regra de agendamento automático de relatórios white-label de um Workspace.
/// </summary>
public sealed class ReportSchedule : Entity<Guid>
{
    private readonly List<ReportRecipient> _recipients = new();

    /// <summary>
    /// Construtor privado para o EF Core.
    /// </summary>
    private ReportSchedule() : base(Guid.Empty)
    {
        Name = string.Empty;
    }

    /// <summary>
    /// Construtor privado para criação controlada via fábrica de domínio.
    /// </summary>
    private ReportSchedule(
        Guid id,
        Guid workspaceId,
        string name,
        ReportFrequency frequency,
        DayOfWeek? dayOfWeek,
        int? dayOfMonth,
        TimeSpan scheduledTimeUtc,
        ReportDateRangeType dateRangeType,
        ReportOutputFormat outputFormat,
        ReportDeliveryChannel deliveryChannels,
        string? customTitle,
        string? customNotes,
        bool includeCopilotInsights,
        bool includeTopCreatives,
        bool includeChannelBreakdown,
        bool includePacingSummary,
        DateTime createdAtUtc)
        : base(id)
    {
        WorkspaceId = workspaceId;
        Name = name;
        Frequency = frequency;
        DayOfWeek = dayOfWeek;
        DayOfMonth = dayOfMonth;
        ScheduledTimeUtc = scheduledTimeUtc;
        DateRangeType = dateRangeType;
        OutputFormat = outputFormat;
        DeliveryChannels = deliveryChannels;
        CustomTitle = customTitle;
        CustomNotes = customNotes;
        IncludeCopilotInsights = includeCopilotInsights;
        IncludeTopCreatives = includeTopCreatives;
        IncludeChannelBreakdown = includeChannelBreakdown;
        IncludePacingSummary = includePacingSummary;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
        NextExecutionAtUtc = CalculateNextExecutionUtc(createdAtUtc);
    }

    /// <summary>
    /// Identificador do Workspace (cliente da agência) ao qual o relatório pertence.
    /// </summary>
    public Guid WorkspaceId { get; private set; }

    /// <summary>
    /// Nome descritivo da regra de agendamento (ex: "Relatório Semanal Executivo").
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Periodicidade de execução (Diário, Semanal ou Mensal).
    /// </summary>
    public ReportFrequency Frequency { get; private set; }

    /// <summary>
    /// Dia da semana planejado para envio quando a frequência for semanal.
    /// </summary>
    public DayOfWeek? DayOfWeek { get; private set; }

    /// <summary>
    /// Dia do mês planejado para envio (1 a 28/31) quando a frequência for mensal.
    /// </summary>
    public int? DayOfMonth { get; private set; }

    /// <summary>
    /// Horário UTC planejado para o disparo do relatório.
    /// </summary>
    public TimeSpan ScheduledTimeUtc { get; private set; }

    /// <summary>
    /// Janela padrão de datas agregadas no relatório.
    /// </summary>
    public ReportDateRangeType DateRangeType { get; private set; }

    /// <summary>
    /// Formato de saída habilitado (Link web interativo, PDF ou ambos).
    /// </summary>
    public ReportOutputFormat OutputFormat { get; private set; }

    /// <summary>
    /// Canais de comunicação ativados para entrega (E-mail, WhatsApp ou ambos).
    /// </summary>
    public ReportDeliveryChannel DeliveryChannels { get; private set; }

    /// <summary>
    /// Título customizado exibido no cabeçalho do relatório (opcional).
    /// </summary>
    public string? CustomTitle { get; private set; }

    /// <summary>
    /// Observação estratégica ou comentário do gestor inserido no relatório.
    /// </summary>
    public string? CustomNotes { get; private set; }

    /// <summary>
    /// Indica se o diagnóstico diário e anomalias do Copiloto IA devem constar no relatório.
    /// </summary>
    public bool IncludeCopilotInsights { get; private set; }

    /// <summary>
    /// Indica se a galeria de top criativos e análise de fadiga deve constar no relatório.
    /// </summary>
    public bool IncludeTopCreatives { get; private set; }

    /// <summary>
    /// Indica se o comparativo detalhado por plataforma (Meta, Google, Bing, TikTok) deve constar.
    /// </summary>
    public bool IncludeChannelBreakdown { get; private set; }

    /// <summary>
    /// Indica se a barra e métricas de consumo de verba (Budget Pacing) devem constar.
    /// </summary>
    public bool IncludePacingSummary { get; private set; }

    /// <summary>
    /// Indica se o agendamento está ativo para disparos automáticos.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Data e hora UTC da última execução do agendamento.
    /// </summary>
    public DateTime? LastExecutedAtUtc { get; private set; }

    /// <summary>
    /// Data e hora UTC da próxima execução agendada.
    /// </summary>
    public DateTime? NextExecutionAtUtc { get; private set; }

    /// <summary>
    /// Data e hora UTC de criação da regra.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Lista de destinatários configurados para este agendamento.
    /// </summary>
    public IReadOnlyList<ReportRecipient> Recipients => _recipients.AsReadOnly();

    /// <summary>
    /// Fábrica para criar uma nova regra de agendamento de relatórios.
    /// </summary>
    public static Result<ReportSchedule> Create(
        Guid id,
        Guid workspaceId,
        string name,
        ReportFrequency frequency,
        DayOfWeek? dayOfWeek,
        int? dayOfMonth,
        TimeSpan scheduledTimeUtc,
        ReportDateRangeType dateRangeType,
        ReportOutputFormat outputFormat,
        ReportDeliveryChannel deliveryChannels,
        string? customTitle,
        string? customNotes,
        bool includeCopilotInsights,
        bool includeTopCreatives,
        bool includeChannelBreakdown,
        bool includePacingSummary,
        IEnumerable<ReportRecipient>? recipients,
        DateTime createdAtUtc)
    {
        if (id == Guid.Empty)
            return Result<ReportSchedule>.Failure(Error.Validation("ReportSchedule.EmptyId", "O identificador do agendamento é obrigatório."));

        if (workspaceId == Guid.Empty)
            return Result<ReportSchedule>.Failure(Error.Validation("ReportSchedule.EmptyWorkspaceId", "O Workspace é obrigatório."));

        if (string.IsNullOrWhiteSpace(name))
            return Result<ReportSchedule>.Failure(Error.Validation("ReportSchedule.EmptyName", "O nome do agendamento é obrigatório."));

        if (frequency == ReportFrequency.Weekly && !dayOfWeek.HasValue)
            return Result<ReportSchedule>.Failure(Error.Validation("ReportSchedule.WeeklyMissingDay", "Para frequência semanal, o dia da semana deve ser informado."));

        if (frequency == ReportFrequency.Monthly)
        {
            if (!dayOfMonth.HasValue || dayOfMonth.Value < 1 || dayOfMonth.Value > 31)
                return Result<ReportSchedule>.Failure(Error.Validation("ReportSchedule.MonthlyInvalidDay", "Para frequência mensal, o dia do mês deve estar entre 1 e 31."));
        }

        var schedule = new ReportSchedule(
            id,
            workspaceId,
            name.Trim(),
            frequency,
            dayOfWeek,
            dayOfMonth,
            scheduledTimeUtc,
            dateRangeType,
            outputFormat,
            deliveryChannels,
            customTitle?.Trim(),
            customNotes?.Trim(),
            includeCopilotInsights,
            includeTopCreatives,
            includeChannelBreakdown,
            includePacingSummary,
            createdAtUtc);

        if (recipients is not null)
        {
            foreach (var recipient in recipients)
            {
                schedule._recipients.Add(recipient);
            }
        }

        return Result<ReportSchedule>.Success(schedule);
    }

    /// <summary>
    /// Atualiza os parâmetros do agendamento.
    /// </summary>
    public Result Update(
        string name,
        ReportFrequency frequency,
        DayOfWeek? dayOfWeek,
        int? dayOfMonth,
        TimeSpan scheduledTimeUtc,
        ReportDateRangeType dateRangeType,
        ReportOutputFormat outputFormat,
        ReportDeliveryChannel deliveryChannels,
        string? customTitle,
        string? customNotes,
        bool includeCopilotInsights,
        bool includeTopCreatives,
        bool includeChannelBreakdown,
        bool includePacingSummary,
        IEnumerable<ReportRecipient> recipients,
        DateTime updatedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(Error.Validation("ReportSchedule.EmptyName", "O nome do agendamento é obrigatório."));

        if (frequency == ReportFrequency.Weekly && !dayOfWeek.HasValue)
            return Result.Failure(Error.Validation("ReportSchedule.WeeklyMissingDay", "Para frequência semanal, o dia da semana deve ser informado."));

        if (frequency == ReportFrequency.Monthly)
        {
            if (!dayOfMonth.HasValue || dayOfMonth.Value < 1 || dayOfMonth.Value > 31)
                return Result.Failure(Error.Validation("ReportSchedule.MonthlyInvalidDay", "Para frequência mensal, o dia do mês deve estar entre 1 e 31."));
        }

        Name = name.Trim();
        Frequency = frequency;
        DayOfWeek = dayOfWeek;
        DayOfMonth = dayOfMonth;
        ScheduledTimeUtc = scheduledTimeUtc;
        DateRangeType = dateRangeType;
        OutputFormat = outputFormat;
        DeliveryChannels = deliveryChannels;
        CustomTitle = customTitle?.Trim();
        CustomNotes = customNotes?.Trim();
        IncludeCopilotInsights = includeCopilotInsights;
        IncludeTopCreatives = includeTopCreatives;
        IncludeChannelBreakdown = includeChannelBreakdown;
        IncludePacingSummary = includePacingSummary;

        _recipients.Clear();
        if (recipients is not null)
        {
            _recipients.AddRange(recipients);
        }

        NextExecutionAtUtc = CalculateNextExecutionUtc(updatedAtUtc);
        return Result.Success();
    }

    /// <summary>
    /// Alterna o estado ativo do agendamento.
    /// </summary>
    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }

    /// <summary>
    /// Registra a execução com sucesso de um disparo e programa a próxima ocorrência.
    /// </summary>
    public void RecordExecution(DateTime executedAtUtc)
    {
        LastExecutedAtUtc = executedAtUtc;
        NextExecutionAtUtc = CalculateNextExecutionUtc(executedAtUtc);
    }

    /// <summary>
    /// Calcula a próxima data e hora UTC de execução a partir de uma data de referência.
    /// </summary>
    public DateTime CalculateNextExecutionUtc(DateTime fromUtc)
    {
        var targetDate = fromUtc.Date;
        var nextCandidate = targetDate.Add(ScheduledTimeUtc);

        switch (Frequency)
        {
            case ReportFrequency.Daily:
                if (nextCandidate <= fromUtc)
                {
                    nextCandidate = targetDate.AddDays(1).Add(ScheduledTimeUtc);
                }
                return nextCandidate;

            case ReportFrequency.Weekly:
                var targetDayOfWeek = DayOfWeek ?? System.DayOfWeek.Monday;
                var daysUntil = ((int)targetDayOfWeek - (int)fromUtc.DayOfWeek + 7) % 7;
                var weeklyCandidate = targetDate.AddDays(daysUntil).Add(ScheduledTimeUtc);
                if (weeklyCandidate <= fromUtc)
                {
                    weeklyCandidate = weeklyCandidate.AddDays(7);
                }
                return weeklyCandidate;

            case ReportFrequency.Monthly:
                var targetDayOfMonth = DayOfMonth ?? 1;
                // Tenta agendar no mês corrente
                var daysInMonth = DateTime.DaysInMonth(targetDate.Year, targetDate.Month);
                var clampedDay = Math.Min(targetDayOfMonth, daysInMonth);
                var monthlyCandidate = new DateTime(targetDate.Year, targetDate.Month, clampedDay, 0, 0, 0, DateTimeKind.Utc).Add(ScheduledTimeUtc);

                if (monthlyCandidate <= fromUtc)
                {
                    // Avança para o próximo mês
                    var nextMonth = targetDate.AddMonths(1);
                    var daysInNextMonth = DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month);
                    var clampedNextDay = Math.Min(targetDayOfMonth, daysInNextMonth);
                    monthlyCandidate = new DateTime(nextMonth.Year, nextMonth.Month, clampedNextDay, 0, 0, 0, DateTimeKind.Utc).Add(ScheduledTimeUtc);
                }
                return monthlyCandidate;

            default:
                return fromUtc.AddDays(1);
        }
    }
}
