namespace BuildingBlocks.Domain.Automations;

/// <summary>
/// Operador lógico para composição de nós na árvore de predicados da DSL de automação.
/// </summary>
public enum LogicalOperator
{
    /// <summary>
    /// Todas as condições filhas devem ser satisfeitas (E lógico).
    /// </summary>
    And = 1,

    /// <summary>
    /// Ao menos uma condição filha deve ser satisfeita (OU lógico).
    /// </summary>
    Or = 2,

    /// <summary>
    /// Inverte a avaliação da condição filha (NÃO lógico).
    /// </summary>
    Not = 3
}

/// <summary>
/// Métrica de desempenho utilizada como critério de avaliação na regra de automação.
/// </summary>
public enum MetricType
{
    /// <summary>
    /// Custo por Aquisição / Conversão (Cost Per Acquisition).
    /// </summary>
    Cpa = 1,

    /// <summary>
    /// Retorno sobre o Investimento em Anúncios (Return On Ad Spend).
    /// </summary>
    Roas = 2,

    /// <summary>
    /// Custo por Clique (Cost Per Click).
    /// </summary>
    Cpc = 3,

    /// <summary>
    /// Custo por Mil Impressões (Cost Per Mille).
    /// </summary>
    Cpm = 4,

    /// <summary>
    /// Taxa de Cliques (Click-Through Rate em porcentagem).
    /// </summary>
    Ctr = 5,

    /// <summary>
    /// Investimento monetário total no período (Spend).
    /// </summary>
    Spend = 6,

    /// <summary>
    /// Volume total de conversões.
    /// </summary>
    Conversions = 7,

    /// <summary>
    /// Receita monetária gerada por conversões (Conversion Value).
    /// </summary>
    ConversionValue = 8
}

/// <summary>
/// Operador de comparação numérica na avaliação de predicados.
/// </summary>
public enum ComparisonOperator
{
    /// <summary>
    /// Maior que (&gt;).
    /// </summary>
    GreaterThan = 1,

    /// <summary>
    /// Maior ou igual a (&gt;=).
    /// </summary>
    GreaterThanOrEqual = 2,

    /// <summary>
    /// Menor que (&lt;).
    /// </summary>
    LessThan = 3,

    /// <summary>
    /// Menor ou igual a (&lt;=).
    /// </summary>
    LessThanOrEqual = 4,

    /// <summary>
    /// Igual a (==).
    /// </summary>
    Equal = 5,

    /// <summary>
    /// Diferente de (!=).
    /// </summary>
    NotEqual = 6
}

/// <summary>
/// Escopo de abrangência do predicado ou da ação na hierarquia de publicidade.
/// </summary>
public enum RuleScope
{
    /// <summary>
    /// Consolidação a nível de todo o Workspace ou plataforma globalmente.
    /// </summary>
    Workspace = 1,

    /// <summary>
    /// Avaliação e atuação a nível de Campanha específica.
    /// </summary>
    Campaign = 2,

    /// <summary>
    /// Avaliação e atuação a nível de Conjunto / Grupo de Anúncios.
    /// </summary>
    AdSet = 3,

    /// <summary>
    /// Avaliação e atuação a nível de Anúncio / Criativo individual.
    /// </summary>
    Ad = 4
}

/// <summary>
/// Tipo de ação de mutação executada quando o gatilho da regra é disparado.
/// </summary>
public enum RuleActionType
{
    /// <summary>
    /// Pausa a veiculação de uma campanha específica ou de todas as campanhas que atenderam o critério.
    /// </summary>
    PauseCampaign = 1,

    /// <summary>
    /// Pausa um anúncio/criativo específico que atendeu o critério.
    /// </summary>
    PauseAd = 2,

    /// <summary>
    /// Reajusta o orçamento diário por um percentual (positivo para aumento, negativo para redução).
    /// </summary>
    AdjustBudgetPercentage = 3,

    /// <summary>
    /// Define um novo orçamento diário fixo absoluto.
    /// </summary>
    AdjustBudgetFixed = 4,

    /// <summary>
    /// Realoca orçamento entre duas campanhas/plataformas (retira de uma origem e transfere para o destino).
    /// </summary>
    ReallocateBudget = 5
}
