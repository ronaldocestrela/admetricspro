# 33. Travas de Segurança Operacional (Overspending & Detector 404/500) com Notificações Multi-Canal

* **Status:** Aceito
* **Data:** 2026-09-16
* **Decisores:** Time de Engenharia e Arquitetura AdMetricsPro
* **Contexto Técnico:** Módulo de Automações (`Modules/Automations`), Subfase 4.2 do Roadmap

---

## 1. Contexto e Motivação

Em operações de tráfego pago em escala (Meta Ads, Google Ads, TikTok Ads, Bing Ads), desvios operacionais podem causar perdas financeiras substanciais em poucos minutos:
1. **Estouro de Orçamento (*Overspending*):** Campanhas configuradas incorretamente, erros nas APIs das plataformas ou algoritmos de lances automáticos agressivos podem consumir o dobro ou triplo da verba diária programada.
2. **Páginas de Destino Quebradas (*Broken Landing Pages*):** Falhas em servidores de e-commerce, expiração de links promocionais, erros 404/500 ou indisponibilidade de hospedagem mantêm anúncios ativos queimando orçamento em páginas inacessíveis.

Era mandatário criar mecanismos automáticos de interrupção preventiva de queima de verba (*circuit breakers*) e despacho de alarmes em tempo real para as equipes operacionais.

---

## 2. Decisão Arquitetural

1. **Desacoplamento de Bounded Contexts via MediatR:** O módulo `Automations` não acessa diretamente `DbContext` nem os adaptadores de rede do módulo `Integrations`. As mutações de pausa ocorrem exclusivamente através do despacho in-memory de comandos:
   - `PauseCampaignCommand(WorkspaceId, CampaignId, Reason)`
   - `PauseAdCommand(WorkspaceId, AdId, Reason)`
2. **Regra de 120% para Overspending:** O serviço `OverspendingGuard` calcula o investimento acumulado no dia em relação ao `DailyBudget`. Caso $\text{Spend} > (\text{DailyBudget} \times 1.20)$, a campanha é pausada imediatamente e um incidente de severidade `Critical` é registrado.
3. **Detector de Landing Pages via HTTP HEAD com Fallback:** O serviço `LandingPageHealthChecker` realiza sondagens HTTP `HEAD` leves nas URLs finais dos anúncios ativos. Caso o servidor responda `405 Method Not Allowed`, é acionado fallback para `GET` (lendo apenas headers). Erros HTTP 4xx, 5xx ou timeouts resultam na pausa preventiva do anúncio e registro de incidente de severidade `Emergency`.
4. **Despacho Resiliente Multi-Canal:** O orquestrador `SecurityAlertDispatcher` dispara alertas em paralelo para quatro canais:
   - **Slack:** Incoming Webhooks com formatação Block Kit;
   - **WhatsApp:** Mensagens urgentes para o telefone comercial do tenant;
   - **E-mail:** Mensagens transacionais estilizadas via `IEmailSender`;
   - **Webhooks Genéricos:** Payloads JSON via HTTP POST para integrações externas.
   Falhas em um canal não abortam os demais canais (tolerância a falhas parciais).
5. **Auditoria e Histórico no Banco do Inquilino:** A entidade `SafetyGuardIncident` persiste todos os incidentes no `TenantDbContext`, permitindo consulta histórica via query CQRS e visualização no painel administrativo.

---

## 3. Consequências e Benefícios

### Positivas:
- **Proteção Financeira Imediata:** Redução de prejuízos operacionais com corte de verba em menos de 1 minuto após detecção;
- **Zero Acoplamento entre Módulos:** Toda comunicação obedece aos contratos in-memory do Kernel Compartilhado;
- **Alta Disponibilidade das Notificações:** Relatório detalhado por canal garantindo que incidentes sejam recebidos mesmo se uma das plataformas terceiras sofrer instabilidade;
- **Performance e Baixo Consumo de Rede:** Uso de HTTP `HEAD` evita download de HTML e ativos pesados nas verificações de integridade.

### Considerações:
- Em provedores de hospedagem com limitação estrita de requisições, o health checker deve respeitar rate-limits configuráveis para grandes volumes de anúncios.
