namespace Master.Infrastructure.Services;

/// <summary>
/// Opções de configuração para o serviço em segundo plano de avaliação da régua de trial.
/// </summary>
public sealed class TrialNoticeOptions
{
    /// <summary>
    /// Nome da seção padrão no appsettings.json.
    /// </summary>
    public const string SectionName = "TrialNotice";

    /// <summary>
    /// Indica se o serviço de avaliação automática em segundo plano está ativo. Padrão: true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Intervalo de execução do ciclo em horas. Padrão: 12 horas.
    /// </summary>
    public int IntervalHours { get; set; } = 12;

    /// <summary>
    /// Indica se um ciclo de avaliação deve ser disparado imediatamente na inicialização do host. Padrão: true.
    /// </summary>
    public bool RunOnStartup { get; set; } = true;
}
