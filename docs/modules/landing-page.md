# Landing page pública — AdMetricsPro

## Objetivo e posicionamento

A rota `/` apresenta o AdMetricsPro como uma plataforma de gestão de tráfego pago que reúne análise multicanal, diagnóstico de criativos, controle de orçamento e organização de equipes. A promessa principal é **clareza para decidir onde investir e controle para reduzir desperdícios**. O público inclui gestores de tráfego, agências de performance, e-commerces e equipes de marketing.

O projeto resolve dois problemas complementares: os gerenciadores de mídia mostram partes da operação e o crescimento da carteira aumenta o esforço manual. Analytics fornece contexto para decidir; Automations oferece regras e limites; Integrations centraliza contas e métricas; Tenants organiza a equipe; Master sustenta acesso, provisionamento e assinatura. A LP traduz essas responsabilidades em benefícios para o visitante.

## Análise dos diferenciais e evidências

| Destaque | Motivo para aparecer na LP | Implementação e limite da promessa |
| --- | --- | --- |
| Detector de Fadiga de Criativos | Ajuda a priorizar a renovação de anúncios em desgaste. É uma dor concreta de quem acompanha campanhas. | `Analytics.Domain/Creatives/AdFatigueDetector.cs`: mínimo de três dias de métricas com impressões; tendência de CTR e frequência; estados saudável, alerta e fatigado. Não prevê vendas nem garante melhoria de CPA. |
| Atribuição Multi-Toque | Evita avaliar toda a jornada apenas pelo canal que encerra a conversão. | `Analytics.Infrastructure/Attribution/AttributionCalculator.cs`: primeiro toque, último toque e modelo linear sobre jornadas fornecidas. A LP não promete coleta universal de todos os contatos. |
| MER Unificado | Amplia a leitura além do ROAS reportado isoladamente por cada rede. | `Analytics.Infrastructure/Blended/BlendedMetricsCalculator.cs`: receita total informada dividida pelo gasto agregado; sem receita total, usa valores de conversão disponíveis. MER não representa lucro nem deduplica automaticamente vendas entre plataformas. |
| Pacing e projeção | Torna visível se o ritmo de gasto está alinhado ao orçamento do período. | `docs/modules/automations-budget-pacing.md`: realizado, desvio e projeção de fechamento. Projeções são estimativas. |
| Travas e páginas de destino | Responde à dor de continuar investindo além do limite ou com uma página indisponível. | `docs/modules/automations-safety-guards.md`: condições e mecanismos de segurança configuráveis. Dependem das integrações, permissões e execução operacional. |
| Copiloto de tráfego | Ajuda a priorizar anomalias de segmentação e disputa de termos entre canais. | Detectores do módulo Analytics e `CopilotDataProvider.cs`. Similaridade de segmentações é uma aproximação; não é medição exata de pessoas em comum. A LP apresenta diagnóstico e sugestão, sem prometer que toda remediação já modifica a rede externa. |
| Workspaces, squads e RBAC | Aumenta a relevância para agências que precisam distribuir clientes e acessos. | `docs/modules/tenants-workspaces.md`, `tenants-squads.md` e `tenants-rbac.md`. Banco dedicado por organização; clientes dentro da organização usam contexto e permissões, não um banco dedicado por workspace. |
| White-label e relatórios | Facilita entregar e apresentar o trabalho com a identidade da agência. | `docs/modules/tenants-white-label.md` e `reports-generator.md`; disponibilidade conforme plano. Não há promessa de SLA ou suporte VIP sem evidência comercial. |
| Quatro redes e métricas sincronizadas | Reduz fragmentação e conferências manuais. | Adaptadores e contratos do módulo Integrations para Meta, Google, Bing e TikTok; OAuth e sincronização por ciclos. Disponibilidade depende de plano, autorização e APIs externas. |

O conteúdo elimina as alegações da LP anterior sobre uma versão específica de IA preditiva, volume financeiro gerenciado, implantação em dois minutos, depoimentos e resultados numéricos sem comprovação no repositório. A seção de confiança usa atributos implementados de governança.

## Estrutura preservada da referência

A referência local é `sendium-lp/Sendium.LandingPage`. A implementação ativa reside no WebApp e usa **Blazor Server .NET 10 com pré-renderização**, mantendo o host e as rotas da aplicação. O projeto WebAssembly da referência não foi adicionado à solução nem transformado no host do produto.

`src/Frontend/WebApp/Components/Pages/Home.razor` monta a seguinte sequência:

| Ordem | Componente | Conteúdo adaptado |
| --- | --- | --- |
| Cabeçalho | `LandingNavbar` | Marca, seis destinos, acesso e CTA; altura de 76px; menu móvel abaixo de 1024px. |
| 1 | `HeroSection` | Promessa central, cinco cápsulas, dois CTAs, três pilares e carrossel de três demonstrações. |
| 2 | `PositioningStrip` | Meta, Google, Bing e TikTok no mesmo contexto. |
| 3 | `ProblemSection` | Seis dores: fragmentação, último clique, desgaste, orçamento, concorrência interna e trabalho manual. |
| 4 | `HowItWorksSection` | Seis passos: cadastro, workspaces, contas, indicadores, diagnóstico e ação. |
| 5 | `ResourcesSection` | Quatro diferenciais e quatro abas com seis recursos cada, totalizando 24 itens. |
| 6 | `AudienceSection` | Quatro públicos com necessidades e benefícios específicos. |
| 7 | `TrustSection` | Painel com escudo e seis atributos de governança. |
| 8 | `PricingSection` | Três planos comparáveis sem valores, convite aos 14 dias grátis e bloco para operações maiores. |
| 9 | `FaqSection` | Seis perguntas, com a primeira aberta e apenas uma resposta expandida por vez. |
| 10 | `CTASection` | Convite ao teste e acesso à plataforma. |
| Rodapé | `LandingFooter` | Marca, produto, plataforma, ajuda e confiança; copyright com ano atual. |

O DOM das seções, grids, superfícies, tipografia Inter/JetBrains Mono, proporção 4:3 do carrossel, timeline, partículas, indicadores neon e comportamento responsivo derivam da referência. Os componentes antigos de cockpit, bento e depoimentos permanecem fora da composição pública.

## Navegação e conversão

- Âncoras: `#hero`, `#como-funciona`, `#recursos`, `#para-quem`, `#planos`, `#faq` e `#seguranca`.
- “Produto” passa a apontar para `#hero`; não existe link para uma seção `#funil` ausente.
- Cabeçalho e timeline permitem consultar planos. Hero, cards comerciais, bloco complementar e CTA final levam a `/onboarding`.
- Acesso à conta leva a `/login`.
- Os cards não pré-selecionam um plano: a escolha é feita no cadastro existente.
- Rodapé usa ajuda, FAQ e governança. Não inventa políticas jurídicas, endereço de suporte, horário de atendimento ou status operacional.

## Oferta e valores no cadastro

A LP mantém os três planos e os CTAs de teste, mas os valores aparecem somente em **Plano & Recursos** no cadastro. Nos espaços que antes exibiam preço e periodicidade, a LP mostra “14 dias grátis” e “sem cartão”.

O cadastro usa valores mensais de referência de R$ 197, R$ 497 e R$ 1.497, alinhados aos defaults de `GetCheckoutPreviewQueryHandler` e `ProcessCheckoutCommandHandler`; sua antiga referência de Enterprise a R$ 1.290 foi substituída. O catálogo pode substituir os defaults, por isso preços e condições continuam sujeitos à confirmação no checkout. Consulte [a experiência de cadastro](tenant-onboarding.md#6-experiência-de-cadastro-alinhada-à-landing-page).

A LP não determina valores cobrados e nenhum pagamento é solicitado ao criar a conta. O teste é de 14 dias sem cartão, com recursos conforme o plano escolhido.

## Assets e isolamento visual

- Modelos de navegação e FAQ: `Models/Landing/LandingContent.cs`.
- CSS compilado da referência: `wwwroot/landing/landing.css`, dentro de `@scope (.admetrics-landing)`.
- Rolagem suave limitada ao documento quando a LP está presente; compensação de cabeçalho de 92px.
- JavaScript: `wwwroot/landing/js/`; módulos no navegador controlam os efeitos por frame e removem observadores, listeners e timers no descarte.
- Imagens locais: `wwwroot/images/landing/{overview,creatives,pacing}.svg` e `brand-mark.svg`.
- Os três visuais são **demonstrações ilustrativas**, identificadas no próprio asset e na legenda; não são screenshots de clientes nem prova de resultados. Substituem imagens e logotipos da Sendium.
- As folhas existentes do dashboard/cadastro continuam disponíveis. O CSS novo não atua fora de `.admetrics-landing`.

`@scope` exige navegador com suporte a CSS Scope. As fontes e ícones mantêm a dependência de Google Fonts da referência. A página possui idioma pt-BR, título e descrição próprios, e seu conteúdo pré-renderizado fica visível antes de inicializar os efeitos.

## Interações e acessibilidade

O carrossel avança a cada quatro segundos, pausa com hover/foco, aba oculta, fora da viewport e movimento reduzido. A timeline revela seis etapas com rolagem ou clique; passos revelados permanecem visíveis. Abas mantêm o indicador neon de 620ms. FAQ usa `aria-expanded` e vínculos entre pergunta e resposta. Menu móvel fecha por seleção, Escape ou retorno ao desktop; Escape restaura foco no botão.

As partículas mantêm o limite de 20–65 elementos e densidade máxima de duas vezes. Movimento reduzido mantém conteúdo e timeline visíveis, suspende efeitos contínuos e usa rolagem imediata. O descarte das instâncias em C# tolera `JSDisconnectedException`, correspondente ao encerramento do circuito Blazor Server.

## Contratos e fronteiras

A LP é conteúdo público de apresentação. `GET /` retorna HTML; não há payload de comando, resposta `Result<T>`, consulta ao banco ou novo endpoint de negócio. Login, cadastro, faturamento e recursos internos continuam usando seus clientes HTTP tipados e os contratos da Web API. Portanto, não há novo endpoint OpenAPI/Scalar nem alteração no isolamento dos tenants nesta tarefa.

## Validação

O teste de estrutura foi escrito e executado contra a LP anterior, obtendo falha antes da implementação (Red). A implementação adaptada passa no mesmo contrato (Green).

```bash
dotnet test tests/UnitTests/Frontend/UnitTests.Frontend.csproj --no-restore
node --test tests/AcceptanceTests/Landing/interop.test.mjs
```

bUnit verifica a sequência de seções, contagens, 24 recursos, âncoras existentes, destinos de conversão, estados de navegação, carrossel, abas, FAQ e condições da oferta. Os testes JavaScript exercitam pausas do carrossel, avanço da timeline, revelação, partículas, indicador neon, Escape e descarte de observadores. A conferência no Chrome headless validou desktop de 1440px, 1024px, 390px e 320px, ausência de overflow horizontal, cabeçalho sem sobreposição em 1024px, troca de abas e slides, FAQ, Escape com restauração de foco, movimento reduzido e navegação ao cadastro sem estilos da LP interferindo na tela. A integração posterior com o onboarding também foi coberta pela suíte de frontend; os 11 testes JavaScript da LP seguem aprovados; nenhuma exceção JavaScript registrada na validação interativa.
