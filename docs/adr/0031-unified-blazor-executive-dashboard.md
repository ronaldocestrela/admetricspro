# 31. Dashboard Executivo Unificado no Blazor Server & Gráficos Vetoriais Nativos

## Contexto

A entrega da **Subfase 3.3** do AdMetricsPro exige um painel de inteligência executiva unificado para gestores de tráfego e agências, consolidando métricas das 4 principais redes (Meta Ads, Google Ads, TikTok Ads e Bing Ads).

Os requisitos demandam:
1. Comparação automática com o período anterior equivalente (Deltas percentuais e valores prévios).
2. Semântica de polaridade invertida para custos (CPC e CPA em queda devem ser sinalizados como melhorias / verdes).
3. Gráficos interativos de tendência temporal, participação de canais e distribuição por dispositivo.
4. Exportação rápida para formatos CSV estruturado e imagem/relatório de alta resolução.
5. Conformidade estrita com o princípio da Regra 9 do `AGENTS.md` (Zero acesso direto a banco no frontend: consumo exclusivo via `HttpClient` e Web API com envelopes `Result<T>`).

Avaliou-se a adoção de bibliotecas externas de gráficos JavaScript pesadas (ex.: Chart.js, ApexCharts via wrappers npm). Contudo, tais wrappers introduzem riscos de concorrência com o SignalR do Blazor Server, desserialização JSON excessiva no circuito, dificuldade de personalização de temas White-Label e problemas de renderização durante SSR interativo.

## Decisão

1. **Separação Arquitetural Rígida:**
   - O módulo de backend `Analytics` expõe o endpoint `GET /api/v1/analytics/dashboard/executive`.
   - O manipulador CQRS `GetExecutiveDashboardQueryHandler` calcula dinamicamente a duração da janela corrente e deduz a janela anterior equivalente com mesma duração, delegando ao calculador de domínio `IExecutiveDashboardCalculator` a computação matemática e deltas.
   - O projeto `WebApp` consome os dados exclusivamente via `IAnalyticsDashboardClientService` (`AnalyticsDashboardClientService`), garantindo zero acoplamento direto com a infraestrutura de banco de dados.

2. **Gráficos em SVG Vetorial Nativo no Blazor:**
   - `PerformanceTrendChart.razor`: Gráfico de área e linha temporal diária calculado diretamente em código C# e renderizado em nós `<svg>` e `<path>`, com gradientes suaves e tooltips nativos.
   - `PlatformShareDonutChart.razor`: Gráfico Donut baseado em `stroke-dasharray` e `stroke-dashoffset` proporcionais à participação de cada canal.
   - `DevicePerformanceBarChart.razor`: Barras comparativas de hardware (Mobile, Desktop, Tablet).

3. **Exportação Desacoplada e Segura:**
   - **CSV:** Gerado deterministicamente em C# com codificação UTF-8 com BOM e disparado via blob nativo em `dashboard-export.js`.
   - **Imagem / Relatório:** Disparo vetorial em alta definição via `window.print()` otimizado para formatos PDF e apresentações executivas.

4. **TDD Integral:**
   - Criação de testes unitários com **bUnit** (`MetricCardsGridTests`, `ExecutiveDashboardViewTests`, `DashboardExportActionsTests`) e **xUnit** (`ExecutiveDashboardCalculatorTests`, `GetExecutiveDashboardQueryTests`, `AnalyticsExecutiveDashboardControllerTests`).

## Consequências

### Positivas
- **Desempenho Extremo:** Os gráficos SVG nativos não bloqueiam a thread de renderização do SignalR e não dependem de scripts volumosos de terceiros.
- **Totalmente White-Label:** As cores dos gráficos e cartões herdam diretamente as variáveis CSS do inquilino (`--tenant-primary`, `--tenant-accent`).
- **Idempotência e Segurança:** A lógica temporal e de deltas reside no backend, mantendo a integridade matemática protegida contra manipulação de clientes.
- **Zero Acesso Direto a Banco:** Respeito absoluto às regras arquiteturais de separação de camadas.

### Neutras / Compensações
- Gráficos vetoriais extremamente complexos (com zoom dinâmico de 1000 pontos) exigirão paginação ou amostragem na query, o que já é mitigado pela granularidade diária padrão do painel executivo.
