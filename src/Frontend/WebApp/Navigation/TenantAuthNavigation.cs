namespace WebApp.Navigation;

/// <summary>
/// Utilitários de navegação entre telas públicas e área autenticada do inquilino.
/// </summary>
public static class TenantAuthNavigation
{
    /// <summary>
    /// Monta a rota de login preservando o caminho atual como <c>returnUrl</c> quando aplicável.
    /// </summary>
    /// <param name="navigation">Gerenciador de navegação do circuito Blazor.</param>
    /// <returns>URI relativa para <c>/login</c>, opcionalmente com query de retorno.</returns>
    public static string BuildLoginUrl(Microsoft.AspNetCore.Components.NavigationManager navigation)
    {
        var relativeUri = navigation.ToBaseRelativePath(navigation.Uri).Trim();

        if (string.IsNullOrWhiteSpace(relativeUri) ||
            relativeUri.Equals("login", StringComparison.OrdinalIgnoreCase) ||
            relativeUri.StartsWith("login?", StringComparison.OrdinalIgnoreCase))
        {
            return "/login";
        }

        var localPath = relativeUri.StartsWith('/') ? relativeUri : "/" + relativeUri;
        return $"/login?returnUrl={Uri.EscapeDataString(localPath)}";
    }

    /// <summary>
    /// Determina o destino pós-login a partir de um <paramref name="returnUrl"/> de query string.
    /// </summary>
    /// <param name="returnUrl">Caminho opcional retornado pelo fluxo de redirecionamento anônimo.</param>
    /// <returns>Caminho local validado ou <c>/dashboard</c> como padrão seguro.</returns>
    public static string ResolvePostLoginDestination(string? returnUrl)
    {
        return IsLocalReturnUrl(returnUrl) ? returnUrl!.Trim() : "/dashboard";
    }

    /// <summary>
    /// Indica se o valor representa um caminho relativo local seguro (sem open redirect).
    /// </summary>
    /// <param name="returnUrl">Valor candidato a destino.</param>
    /// <returns>Verdadeiro para caminhos que iniciam com <c>/</c> e não com <c>//</c>.</returns>
    public static bool IsLocalReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return false;
        }

        var trimmed = returnUrl.Trim();
        return trimmed.StartsWith('/') && !trimmed.StartsWith("//", StringComparison.Ordinal);
    }
}