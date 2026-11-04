namespace WebApp.Services.Social;

/// <summary>
/// Modo de visualização da grade do calendário orgânico.
/// </summary>
public enum SocialCalendarView
{
    /// <summary>
    /// Grade mensal com dias de preenchimento do mês anterior e seguinte.
    /// </summary>
    Month,

    /// <summary>
    /// Grade semanal com sete colunas iniciando na segunda-feira.
    /// </summary>
    Week,
}
