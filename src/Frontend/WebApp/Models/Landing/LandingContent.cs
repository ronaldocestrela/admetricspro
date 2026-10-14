namespace WebApp.Models.Landing;

/// <summary>Destino de navegação dentro da landing page pública.</summary>
/// <param name="id">Identificador da seção renderizada.</param>
/// <param name="label">Texto exibido na navegação.</param>
internal sealed class NavigationLink(string id, string label)
{
    /// <summary>Identificador da seção renderizada.</summary>
    public string Id { get; } = id;

    /// <summary>Texto exibido na navegação.</summary>
    public string Label { get; } = label;
}

/// <summary>Pergunta e resposta sobre as capacidades reais do produto.</summary>
/// <param name="id">Identificador estável do item.</param>
/// <param name="question">Dúvida apresentada ao visitante.</param>
/// <param name="answer">Explicação da capacidade e de suas dependências.</param>
internal sealed class FaqItem(int id, string question, string answer)
{
    /// <summary>Identificador estável do item.</summary>
    public int Id { get; } = id;

    /// <summary>Dúvida apresentada ao visitante.</summary>
    public string Question { get; } = question;

    /// <summary>Capacidade e dependências explicadas na resposta.</summary>
    public string Answer { get; } = answer;
}

/// <summary>Conteúdo editorial da navegação e das dúvidas da landing page.</summary>
internal static class LandingContent
{
    /// <summary>Âncoras das seções efetivamente renderizadas.</summary>
    public static IReadOnlyList<NavigationLink> Navigation { get; } =
    [
        new("hero", "Produto"),
        new("como-funciona", "Como funciona"),
        new("recursos", "Recursos"),
        new("para-quem", "Para quem é"),
        new("planos", "Planos"),
        new("faq", "FAQ")
    ];

    /// <summary>Respostas sobre integrações, análise, governança e adoção.</summary>
    public static IReadOnlyList<FaqItem> Faqs { get; } =
    [
        new(1, "Quais plataformas posso conectar?", "O AdMetricsPro reúne Meta Ads, Google Ads, Bing Ads e TikTok Ads. As redes disponíveis dependem do plano. A conexão usa autorização oficial por OAuth; a atualização dos indicadores acompanha os ciclos de sincronização e a disponibilidade das APIs de cada plataforma."),
        new(2, "Como o Detector de Fadiga de Criativos funciona?", "A análise precisa de pelo menos três dias de métricas com impressões. Ela cruza a tendência do CTR com a frequência de exposição e classifica o criativo como saudável, em alerta ou fatigado. Você recebe contexto para preparar variações ou renovar a peça; a detecção não garante um resultado futuro nem substitui sua avaliação."),
        new(3, "O que a Atribuição Multi-Toque e o MER mostram?", "A atribuição compara primeiro toque, último toque e modelo linear nas jornadas de conversão fornecidas, destacando a contribuição dos canais. O MER relaciona a receita total informada ao investimento consolidado em mídia. Sem receita total, o cálculo usa os valores de conversão disponíveis; a análise depende da qualidade e da cobertura desses dados."),
        new(4, "As automações assumem o controle das minhas campanhas?", "Você define condições, ações e limites. A plataforma oferece regras de desempenho, pacing, travas de gasto e verificações de páginas de destino. O Copiloto apresenta diagnósticos e sugestões para revisão. A execução depende das permissões, conexões e ações suportadas por cada integração; acompanhe os registros e as configurações da operação."),
        new(5, "Posso trabalhar com vários clientes e usar a marca da agência?", "Sim. Workspaces organizam os clientes, squads distribuem carteiras e papéis delimitam acessos. Cada organização possui um banco dedicado; os clientes dentro dela são organizados por contexto e permissões. White-label, domínio próprio e relatórios personalizados estão disponíveis conforme os recursos contratados."),
        new(6, "Como começo e o que acontece depois do teste?", "Use o botão de teste grátis para acessar o cadastro, escolher o plano e criar sua operação. O período de teste é de 14 dias, sem cartão. Depois, a ativação da assinatura é realizada no faturamento da plataforma. Preços, limites, recursos e condições vigentes são confirmados no cadastro e no checkout.")
    ];
}
