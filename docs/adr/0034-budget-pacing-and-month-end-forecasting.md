# 34. Gestão Dinâmica de Budget & Previsão de Fim de Mês (Pacing)

* **Status:** Aceito
* **Data:** 2026-09-16
* **Decisores:** Time de Engenharia e Arquitetura AdMetricsPro
* **Contexto Técnico:** Módulo de Automações (`Modules/Automations`), Subfase 4.3 do Roadmap

---

## 1. Contexto e Motivação

Na gestão profissional de mídia paga (Meta Ads, Google Ads, TikTok Ads, Bing Ads), gerenciar o ritmo de consumo de verba é um dos maiores desafios operacionais de agências e anunciantes:
1. **Subinvestimento (*Underspending*):** Entregar menos que a verba contratada pelo cliente no fechamento do mês gera insatisfação comercial, quebra de contratos e perda de receita por taxa de administração.
2. **Esgotamento Prematuro (*Overpacing*):** Consumir o orçamento antes do encerramento do mês força a interrupção prematura de campanhas, desestabilizando o aprendizado dos algoritmos de lances e deixando a marca sem presença em dias estratégicos.
3. **Falta de Previsibilidade Linear:** Calcular manualmente o *Run-Rate* diário de dezenas de clientes e centenas de campanhas é inviável e sujeito a erros.

Era indispensável implementar um motor analítico padronizado de projeção de consumo (*Forecast*) e classificação semântica de ritmo, acoplado a componentes visuais de fácil assimilação.

---

## 2. Decisão Arquitetural

1. **Calculador de Domínio Puro (`BudgetPacingCalculator`):**
   - Implementado como serviço de domínio desacoplado de I/O em `Automations.Domain.Pacing`.
   - Adota projeção linear baseada no ritmo médio realizado ($S_{\text{current}} / N_{\text{elapsed}}$) multiplicada pelos dias totais do ciclo.
   - Retorna o record inalterável `BudgetPacingCalculationResult` através do padrão estrito `Result<T>`.

2. **Classificação em 3 Status Operacionais com Margem de Tolerância:**
   - **No Ritmo (`OnTrack`):** $\text{PacingRatio}$ entre $90\%$ e $110\%$ ($0.90 \le \text{Ratio} \le 1.10$). O consumo está perfeitamente calibrado com a data atual.
   - **Sobreaquecido (`Over`):** $\text{PacingRatio} > 1.10$. A velocidade de consumo está acelerada, exigindo redução do orçamento diário restante.
   - **Subinvestido (`Under`):** $\text{PacingRatio} < 0.90$. A velocidade de consumo está lenta, exigindo aumento da verba diária ou realocação para campanhas de maior tração.

3. **Cálculo de Ritmo Diário Recomendado:**
   - O calculador projeta a taxa diária necessária nos dias restantes para que a conta atinja exatamente 100% da verba contratada:
     $$\text{DailyRunRate}_{\text{recommended}} = \frac{\max(0, \text{TargetBudget} - S_{\text{current}})}{\max(1, N_{\text{remaining}})}$$

4. **Isolamento de Persistência no Monólito Modular:**
   - `Automations.Application` declara o contrato `IBudgetPacingDataProvider`, mantendo zero referências a Entity Framework Core.
   - `Automations.Infrastructure` implementa o provedor consultando o banco do inquilino contextual (`TenantDbContext`) via `ITenantDbContextAccessor`.

5. **Exposição RESTful e Documentação Viva:**
   - `BudgetPacingController.cs` expõe rotas versionadas `/api/v1/automations/pacing/...` para consulta individual por cliente, listagem consolidada de portfólio da agência e simulações ad-hoc.

6. **Frontend Blazor Server Exclusivo de Apresentação:**
   - Consumo estritamente via `BudgetPacingClientService` injetado por `HttpClient` tipado.
   - Componente visual `BudgetPacingBar.razor` renderiza barra de progresso multicamada com cores semânticas (verde esmeralda, carmim e âmbar), agulha temporal (needle) do gasto ideal e projeção de fechamento tracejada.

---

## 3. Consequências e Benefícios

### Positivas:
- **Visibilidade Instantânea:** O gestor identifica desvios de orçamento em segundos sem recorrer a planilhas externas;
- **Recomendações Táticas Automatizadas:** Emissão imediata do ritmo diário recomendado em R$/dia para os dias restantes;
- **Conformidade com AGENTS.md:** Zero exceções para regras de negócio, separação física de camadas e bUnit para testes visuais;
- **Preparação para Fase 5:** Fornece a base de dados preditiva necessária para a realocação dinâmica de fim de ciclo e ações em lote.

### Considerações:
- No primeiro dia do ciclo, o gasto pode ter volatilidade estatística natural antes que uma média representativa de dias seja consolidada. O componente trata `N_elapsed >= 1` para evitar divisão por zero.
