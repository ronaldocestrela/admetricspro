# Especificação de Funcionalidades — SaaS de Gestão Unificada de Tráfego

Este documento detalha o escopo de funcionalidades, diferenciais e arquitetura funcional para uma plataforma SaaS integrada às APIs de **Meta Ads**, **Google Ads**, **Microsoft Advertising (Bing Ads)** e **TikTok Ads**.

---

## 1. Visão Geral do Produto

O sistema opera como uma camada centralizada de consolidação, automação e inteligência de mídia paga, eliminando a dispersão operacional entre múltiplos gerenciadores e viabilizando a tomada de decisões orientadas a dados consolidados.

### Redes Integradas
* **Meta Ads API** (Facebook & Instagram)
* **Google Ads API** (Search, Performance Max, Display, YouTube)
* **Microsoft Advertising API** (Bing Search & Audience Network)
* **TikTok Marketing API** (Feed, TopView, Spark Ads)

---

## 2. Métricas e Centralização Cross-Network

### 2.1 Dashboard Unificado Multiplataforma
* **Visão Consolidada em Tempo Real:** Agrupamento de métricas-chave normalizadas em um único painel:
  * Investimento Total (*Spend*)
  * Impressões e Alcance
  * Cliques e Taxa de Clique (CTR)
  * Custo por Clique (CPC) e Custo por Mil Impressões (CPM)
  * Conversões Globais e Custo por Aquisição (CPA)
  * Retorno sobre Investimento Publicitário (ROAS)
* **Filtros Globais Interativos:** Segmentação rápida por cliente/workspace, período, objetivo de campanha e rede de veiculação.
* **Conversão Cambial Automática:** Normalização de moedas para contas faturadas em BRL, USD ou EUR.

### 2.2 Atribuição Multicanal & Blended ROAS
* **MER (*Marketing Efficiency Ratio*):** Indicador executivo que calcula a Receita Total do Negócio / Investimento Total em Mídia.
* **Blended CAC & Blended ROAS:** Apuração do custo de aquisição e retorno sem o viés inflacionado de atribuição individual de cada rede.
* **Modelos de Atribuição Cross-Network:** Comparação entre modelos (Primeiro Clique, Último Clique, Linear e Baseado em Dados) para visualizar como canais de topo (ex.: TikTok) alimentam canais de fundo de funil (ex.: Google/Bing).

### 2.3 Taxonomia e Padronização de Dados
* **Tagging Automático por Funil:** Classificação padronizada de campanhas (Topo, Meio, Fundo de Funil, Retargeting) independentemente do padrão de nomenclatura usado na origem.
* **Gerador Centralizado de UTMs:** Construtor integrado de links com convenções rígidas de parâmetros para garantir integridade analítica no GA4 e ferramentas de CRM.

---

## 3. Automação e Regras Inteligentes

### 3.1 Motor de Regras Cross-Platform
* **Gatilhos Condicionais Customizados (If/Then):**
  * *Rebalanceamento de Canais:* "Se CPA no TikTok Ads > R$ 45 nas últimas 48h E Google Ads ROAS > 4.5, reduzir o orçamento diário do TikTok em 20% e alocar o saldo na campanha PMax do Google."
  * *Corte Rápido de Queima:* "Se gasto do anúncio atingir R$ 100 sem conversões nas últimas 24h, pausar anúncio e disparar notificação imediata."
* **Ajustes de Lances em Lote:** Otimização programada de lances com base no histórico de conversão por dia/horário.

### 3.2 Travas de Segurança e Monitoramento Ativo
* **Proteção contra Overspending:** Monitoramento de taxas anômalas de gasto para prevenir que falhas de configuração estourem o orçamento diário.
* **Monitor de Integridade de Links (404/500 Detector):** Teste periódico das URLs finais dos anúncios ativos. Caso o site ou página de vendas apresente erro, a campanha/anúncio é pausado preventivamente.
* **Alerta Instantâneo de Reprovações:** Notificação em tempo real quando criativos ou copys forem rejeitados pelas políticas de qualquer uma das redes.

### 3.3 Gestão Dinâmica de Orçamento (*Pacing*)
* **Previsão de Fechamento de Mês:** Projeção diária do consumo da verba contratada para evitar sub-investimento (*underspending*) ou esgotamento prematuro do orçamento.
* **Realocação Dinâmica de Fim de Ciclo:** Redistribuição automática de saldos residuais não consumidos para as campanhas de melhor tração na reta final do mês.

---

## 4. Operações em Lote e Gestão de Criativos

### 4.1 Criação e Publicação Multiplataforma
* **Fluxo Unificado de Criação:** Configuração de campanha e segmentação básica a partir de uma interface única com publicação simultânea para as redes selecionadas.
* **Smart Resizer de Mídia:** Adaptação automatizada de criativos para formatos verticais (9:16 - TikTok/Reels/Shorts), quadrados (1:1 - Feed) e horizontais (16:9 - YouTube/Display).
* **Variações Dinâmicas de Copy:** Sugestão e ajuste de títulos, descrições e CTAs de acordo com o limite de caracteres e tom de voz de cada plataforma.

### 4.2 Edição e Operações em Massa
* Alteração em lote de status (ativar/pausar), datas de término e orçamentos diários em dezenas de campanhas de diferentes redes em uma única ação.
* Pré-visualização de impacto antes da execução em massa.

### 4.3 Creative Hub (Repositório de Ativos)
* Biblioteca central de imagens e vídeos com métricas de desempenho consolidadas por ativo.
* **Detecção de Fadiga de Criativos:** Alertas acionados quando o CTR de um criativo cair consecutivamente e a frequência média de exibição ultrapassar níveis saudáveis.

---

## 5. Relatórios, Inteligência Artificial e White-Label

### 5.1 Relatórios Automatizados e Portais de Clientes
* **Relatórios Programados:** Envio automático de relatórios em PDF ou links interativos para clientes (frequência diária, semanal ou mensal).
* **White-Label Total:**
  * Domínio personalizado (ex.: `analytics.suaagencia.com.br`).
  * Identidade visual personalizada (logotipo, cores institucionais e assinaturas).
* **Canais de Distribuição:** Disparo via e-mail, Slack ou WhatsApp.

### 5.2 Copiloto de Otimização via IA
* **Auditor Diário em Linguagem Natural:** Síntese diária destacando vitórias, riscos e anomalias de desempenho.
* **Detecção de Canibalização:** Alerta sobre concorrência interna entre públicos semelhantes no Meta Ads e disputa de palavras-chave equivalentes entre Google e Bing Ads.
* **Ações Sugeridas em 1 Clique:** Sugestões práticas de otimização apresentadas com botão para execução direta na API.

---

## 6. Arquitetura Multitenant e Gestão de Acessos

* **Estrutura Hierárquica:** Agência / Empresa Principal > Clientes / Projetos > Contas de Anúncio Conectadas.
* **Controle de Permissões (RBAC):**
  * *Administrador Geral:* Acesso total e gerenciamento de faturamento.
  * *Gestor / Analista de Tráfego:* Permissão para criar, editar, pausar campanhas e regras.
  * *Cliente / Visualizador:* Acesso exclusivo para leitura de dashboards e relatórios (sem visualização de custos internos ou margem).
* **Log de Auditoria:** Histórico detalhado de todas as alterações manuais e disparos de regras automatizadas.