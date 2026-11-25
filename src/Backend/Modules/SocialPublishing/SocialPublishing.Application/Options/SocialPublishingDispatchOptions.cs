namespace SocialPublishing.Application.Options;

/// <summary>
/// Opções do worker de disparo de publicação orgânica agendada.
/// </summary>
public sealed class SocialPublishingDispatchOptions
{
    /// <summary>
    /// Nome da seção de configuração.
    /// </summary>
    public const string SectionName = "SocialPublishing:Dispatch";

    /// <summary>
    /// Habilita o ciclo de disparo; desligado por padrão.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Intervalo mínimo entre ciclos completos, em segundos.
    /// </summary>
    public int IntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Tempo máximo em segundos antes de devolver um job <c>InProgress</c> órfão a retentativa.
    /// </summary>
    public int InProgressTimeoutSeconds { get; set; } = 600;
}