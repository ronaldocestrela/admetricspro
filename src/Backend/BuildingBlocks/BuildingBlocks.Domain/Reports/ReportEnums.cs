namespace BuildingBlocks.Domain.Reports;

/// <summary>
/// Define a periodicidade de execução e disparo de um relatório agendado.
/// </summary>
public enum ReportFrequency
{
    /// <summary>
    /// Disparo diário na hora configurada.
    /// </summary>
    Daily = 1,

    /// <summary>
    /// Disparo semanal no dia da semana especificado.
    /// </summary>
    Weekly = 2,

    /// <summary>
    /// Disparo mensal no dia do mês especificado.
    /// </summary>
    Monthly = 3
}

/// <summary>
/// Define o intervalo de datas padrão aplicado à consolidação das métricas do relatório.
/// </summary>
public enum ReportDateRangeType
{
    /// <summary>
    /// Últimos 7 dias retroativos.
    /// </summary>
    Last7Days = 1,

    /// <summary>
    /// Últimos 14 dias retroativos.
    /// </summary>
    Last14Days = 2,

    /// <summary>
    /// Últimos 30 dias retroativos.
    /// </summary>
    Last30Days = 3,

    /// <summary>
    /// Mês anterior consolidado fechado.
    /// </summary>
    PreviousMonth = 4,

    /// <summary>
    /// Do dia 1º do mês corrente até a data atual (Month-to-Date).
    /// </summary>
    MonthToDate = 5
}

/// <summary>
/// Canal de entrega de relatórios para os destinatários da agência/cliente.
/// </summary>
public enum ReportDeliveryChannel
{
    /// <summary>
    /// Envio por E-mail com template corporativo e PDF anexo.
    /// </summary>
    Email = 1,

    /// <summary>
    /// Envio por WhatsApp corporativo via webhook com resumo e link público.
    /// </summary>
    WhatsApp = 2,

    /// <summary>
    /// Envio conjunto por E-mail e WhatsApp.
    /// </summary>
    Both = 3
}

/// <summary>
/// Formato de saída disponibilizado pelo relatório.
/// </summary>
public enum ReportOutputFormat
{
    /// <summary>
    /// Apenas link web interativo seguro com expiração.
    /// </summary>
    InteractiveWebLink = 1,

    /// <summary>
    /// Apenas arquivo binário PDF para download/anexo.
    /// </summary>
    Pdf = 2,

    /// <summary>
    /// Ambos os formatos: Link web interativo e PDF executivo.
    /// </summary>
    Both = 3
}

/// <summary>
/// Status do ciclo de vida de um relatório gerado.
/// </summary>
public enum ReportStatus
{
    /// <summary>
    /// Relatório compilado com sucesso, aguardando ou sem disparo.
    /// </summary>
    Generated = 1,

    /// <summary>
    /// Relatório despachado com sucesso aos destinatários.
    /// </summary>
    Dispatched = 2,

    /// <summary>
    /// Falha durante a compilação ou entrega multi-canal.
    /// </summary>
    Failed = 3
}
