namespace WebApp.Theme;

/// <summary>
/// Resolve o identificador de tema (<c>light</c> ou <c>dark</c>) para o atributo <c>data-theme</c> do documento.
/// </summary>
public static class ThemePreferenceResolver
{
    /// <summary>
    /// Chave usada no <c>localStorage</c> do navegador para persistir a escolha do usuário.
    /// </summary>
    public const string StorageKey = "admetrics-theme";

    /// <summary>
    /// Valor canônico do tema claro.
    /// </summary>
    public const string Light = "light";

    /// <summary>
    /// Valor canônico do tema escuro.
    /// </summary>
    public const string Dark = "dark";

    /// <summary>
    /// Determina o tema efetivo com prioridade: preferência salva, depois sistema, depois claro.
    /// </summary>
    /// <param name="savedPreference">Valor lido do armazenamento local (<c>light</c> ou <c>dark</c>), se houver.</param>
    /// <param name="systemPrefersDark">Indica se o SO/navegador prefere esquema escuro; <c>null</c> quando indisponível.</param>
    /// <returns><see cref="Light"/> ou <see cref="Dark"/>.</returns>
    public static string Resolve(string? savedPreference, bool? systemPrefersDark)
    {
        if (string.Equals(savedPreference, Light, StringComparison.OrdinalIgnoreCase))
        {
            return Light;
        }

        if (string.Equals(savedPreference, Dark, StringComparison.OrdinalIgnoreCase))
        {
            return Dark;
        }

        if (systemPrefersDark == true)
        {
            return Dark;
        }

        if (systemPrefersDark == false)
        {
            return Light;
        }

        return Light;
    }
}