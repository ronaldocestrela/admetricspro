namespace Analytics.Domain.Taxonomy;

/// <summary>
/// Define os estágios padronizados do funil de marketing e conversão para classificação de tráfego pago.
/// </summary>
public enum FunnelStage
{
    /// <summary>
    /// Topo de funil / Prospecção / Atração / Reconhecimento de Marca (ex: TOF, Topo, Prospecting).
    /// </summary>
    Top = 1,

    /// <summary>
    /// Meio de funil / Engajamento / Consideração / Tráfego Qualificado (ex: MOF, Meio, Consideration).
    /// </summary>
    Middle = 2,

    /// <summary>
    /// Fundo de funil / Conversão / Remarketing / Vendas diretas (ex: BOF, Fundo, Remarketing, RMK).
    /// </summary>
    Bottom = 3,

    /// <summary>
    /// Retenção / Pós-Venda / Reativação de clientes / LTV (ex: RET, Retenção, Reativação, Churn).
    /// </summary>
    Retention = 4,

    /// <summary>
    /// Não classificado ou sem convenção reconhecível na nomenclatura.
    /// </summary>
    Unclassified = 99
}
