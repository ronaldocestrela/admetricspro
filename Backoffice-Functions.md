# Especificação de Funcionalidades — Backoffice Global (Super Admin)

Este documento detalha o escopo de funcionalidades, governança operacional e ferramentas administrativas do **Backoffice** para a equipe interna do SaaS de Gestão Unificada de Tráfego Pago.

---

## 1. Visão Geral do Módulo

O Backoffice é o centro de comando restrito aos operadores, suporte, desenvolvedores e diretores do SaaS. Sua função primária é gerenciar o ciclo de vida dos tenants, garantir a estabilidade das conexões com Meta Ads, Google Ads, Bing Ads e TikTok Ads, controlar planos e acompanhar a saúde financeira do negócio.

---

## 2. Gestão Global de Tenants e Clientes

### 2.1 Diretório Geral e Visão 360º
* **Listagem Centralizada de Tenants:** Filtros ágeis por status (Ativo, Em Período de Teste/Trial, Inadimplente, Suspenso, Cancelado).
* **Ficha Técnica da Empresa Assinante:**
  * Dados cadastrais completos (CNPJ/Tax ID, Razão Social, endereço fiscal, contato do gestor da conta).
  * Plano atual contratado, data do ciclo de faturamento e método de pagamento.
  * Mapeamento de uso: total de Workspaces/Clientes cadastrados, número de assentos em uso e volume total de investimento em mídia sincronizado no ciclo.
* **Intervenções Administrativas:**
  * Bloqueio ou desconexão forçada de tenants suspeitos de fraude ou violação de termos de uso.
  * Concessão manual de extensão de trial ou aplicação de descontos contratuais customizados.

### 2.2 Impersonation (Suporte "Shadow Mode")
* **Acesso Técnico Seguro:** Capacidade do operador de suporte acessar o ambiente do tenant exatamente com a visão do usuário, sem solicitar ou resetar a senha do cliente.
* **Governança de Impersonation:**
  * Exigência de preenchimento de justificativa/número de ticket antes do login.
  * Registro imutável de todas as ações tomadas durante a sessão de suporte na trilha de auditoria global.
  * Ocultação de dados bancários/cartões do tenant durante a visualização de suporte.

---

## 3. Gestão Financeira, Planos e Precificação (Billing Master)

### 3.1 Construtor de Planos e Tiers
* **Parametrização Flexível de Planos:**
  * Definição de limites estruturais: cotas de usuários (assentos), limites de workspaces/clientes e teto de verba mensal gerenciada (*Ad Spend Cap*).
  * Chaveamento de recursos avançados: liberação seletiva de white-label, domínio personalizado (CNAME), acesso a regras automáticas e copiloto de IA.
* **Modelos de Cobrança Suportados:**
  * Recorrência fixa (mensal / semestral / anual com desconto).
  * Modelo híbrido (Mensalidade base + percentual sobre o ad spend excedente).
  * Catálogo de Add-ons avulsos (pacotes de usuários extras ou workspaces adicionais).

### 3.2 Indicadores Financeiros e Prevenção de Churn
* **Métricas SaaS em Tempo Real:**
  * MRR (*Monthly Recurring Revenue*) e ARR (*Annual Recurring Revenue*).
  * Taxa de Churn (Logo Churn e Net Revenue Churn).
  * LTV (*Customer Lifetime Value*) e Ticket Médio (ARPU).
* **Régua de Cobrança e Dunning Automatizado:**
  * Controle de retentativas automáticas em pagamentos recusados.
  * Política de bloqueio funcional progressivo (ex.: D+3 desativa regras de automação, D+7 bloqueia relatórios, D+14 suspende o login).

---

## 4. Hub de Monitoramento de APIs e Infraestrutura

A estabilidade da plataforma depende diretamente das cotas de consumo e da saúde das APIs externas.

### 4.1 Monitor de Rate Limits e Quotas
* **Painel de Consumo por Provedor:**
  * **Meta Graph API:** Percentual de consumo de chamadas por hora por app.
  * **Google Ads API:** Monitoramento diário de Developer Token Operations.
  * **TikTok Marketing API & Bing Ads API:** Taxa de requisições por segundo/minuto.
* **Alertas de Risco:** Disparo de notificações internas quando o consumo global ultrapassar 80% dos limites concedidos pelas redes.

### 4.2 Detector de Conexões Quebradas e Sincronização
* **Central de Tokens Vencidos:** Identificação imediata de contas de anúncio com permissões OAuth revogadas (ex.: quando o cliente final troca a senha da conta de anúncios).
* **Painel da Fila de Ingestão (ETL Queue):** Rastreamento de atrasos ou congestionamento nas filas de sincronização de relatórios e métricas periódicas.

---

## 5. Feature Flags e Comunicação com o Ecossistema

### 5.1 Controle de Funcionalidades (Feature Flags)
* **Lançamento Gradual (Rollout Progressivo):** Liberação de módulos beta (ex.: novo motor de automação cross-network) para grupos selecionados de clientes antes do lançamento público.
* **Kill Switch Operacional:** Desativação instantânea de qualquer módulo que apresente anomalias críticas (ex.: congelamento temporário do disparador de regras se uma API externa passar por instabilidade).

### 5.2 Avisos Globais de Sistema (System Banners)
* Publicação de banners informativos no topo da tela de todos os tenants (ex.: manutenções programadas ou instabilidade confirmada no Facebook Ads).

---

## 6. Trilha de Auditoria Master e Controle de Acessos Internos

* **Matriz de Permissões Internas (Equipe do SaaS):**
  * *Nível 1 (Suporte):* Visualização de contas, status e impersonation com restrições.
  * *Nível 2 (Operações / Financeiro):* Gestão de faturamento, planos, faturas e cancelamentos.
  * *Nível 3 (Super Admin / Engenharia):* Acesso a logs de infraestrutura, controle de feature flags e rate limits.
* **Segurança e Log Master:**
  * Exigência obrigatória de 2FA e login via provedor corporativo (SSO) para colaboradores internos.
  * Histórico perpétuo de qualquer alteração de plano, prorrogação de prazo ou intervenção técnica em contas de clientes.