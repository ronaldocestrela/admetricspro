# ADR 0014: Monitoramento de Rate Limits de APIs e Rastreamento Preventivo de Quotas

## Status
Aceito

## Contexto
A plataforma AdMetricsPro integra-se de forma contínua e massiva às APIs de quatro gigantes de publicidade digital: Meta Ads, Google Ads, TikTok Ads e Bing Ads. Cada uma dessas plataformas adota políticas rígidas e distintas de quotas operacionais e *rate limiting* por app de desenvolvedor e por conta de anúncios:
- Meta: Limites de chamadas e tempo de CPU calculados via headers `X-App-Usage`.
- Google: Limites diários de operações por Developer Token e QPS por MCC.
- TikTok: Limite de requisições por segundo (QPS) e quotas diárias por App ID.
- Bing: Chamadas por minuto e cotas diárias por credencial de anunciante.

Se a plataforma ultrapassar os limites ou operar com tokens de clientes expirados:
1. Ocorre bloqueio por saturação (*throttling*), prejudicando o corte automático de gastos (*overspending*) e regras críticas de automação.
2. Tokens OAuth corporativos correm o risco de rebaixamento de nível ou suspensão temporária.
3. Inquilinos enfrentam interrupção silenciosa na coleta de métricas e divergência de relatórios.

Necessita-se de um agregador centralizado que:
1. Rastreie em tempo real o volume consumido em relação ao teto.
2. Dispare alertas preventivos mandatários ao atingir **80% do teto** para que a equipe possa mitigar o problema antes que o bloqueio aconteça.
3. Rastreie a validade dos tokens dos inquilinos, sinalizando credenciais vencendo em menos de 7 dias ou desconectadas.

## Decisão

1. **Agregador de Domínio `ApiQuotaTracker` (`Master.Domain.Integrations`):**
   - Criação da entidade agregada com controle de estado de alerta (`QuotaAlertLevel`: `Normal`, `Warning`, `Critical`, `Exceeded`).
   - Implementação da trava preventiva de 80% (`DefaultWarningThreshold = 80.0%`), emitindo o evento de domínio `ApiQuotaThresholdWarningEvent` ao cruzar a marca.
   - Escalonamento para `Critical` ao atingir 95% e `Exceeded` em 100%.
   - Suporte a janelas de cota rotativas (`ResetWindow`) e recálculo de limites (`UpdateLimits`).

2. **Entidade de Saúde de Conexões de Tenants (`TenantApiConnection`):**
   - Rastreamento dos status de tokens OAuth (`Connected`, `ExpiringSoon`, `Expired`, `Revoked`, `Disconnected`).
   - Avaliação automática da janela de expiração de 7 dias (`warningWindow = TimeSpan.FromDays(7)`).

3. **Arquitetura Híbrida de Telemetria (Em Memória + Persistência Relacional):**
   - Serviço de rastreamento em memória de alta performance e thread-safe (`InMemoryApiQuotaTracker`) para não onerar o banco de dados em cada chamada HTTP externa.
   - Persistência e snapshots no catálogo central (`MasterDbContext`) com as tabelas `ApiQuotaTrackers` e `TenantApiConnections`.
   - Migração EF Core versionada `20260903180000_Add_ApiHealthAndQuotaTracking`.

4. **API Rest Administrativa no Host WebApi:**
   - Controlador `ApiHealthController` disponibilizando `GET /api/v1/admin/api-health`, `GET /api/v1/admin/api-health/connections` e `POST /api/v1/admin/api-health/usage`.
   - Retornos envelopados no padrão estrito `Result<T>` com contratos OpenAPI e Scalar UI versionados.

5. **Painel Interativo Blazor Server (`ApiHealthDashboard.razor`):**
   - Painel operacional acessível na rota `/admin/api-health` com cartões de indicadores de consumo com barra de progresso colorida (verde, amarelo e vermelho).
   - Tag de aviso destacada em amarelo quando a cota atinge 80%+.
   - Grid filtrável com identificação de inquilinos com tokens expirados ou revogados.

## Consequências

### Positivas
- **Prevenção Ativa de Interrupções:** A emissão de alertas aos 80% do limite concede à engenharia e ao suporte tempo hábil para distribuir requisições ou negociar ampliação de cota antes do *throttling*.
- **Desacoplamento e Baixa Latência:** A utilização de cache e contadores thread-safe em memória desacopla o tráfego de medição de chamadas da camada de I/O em disco.
- **Governança de Inquilinos:** Visibilidade antecipada de tokens que expirarão na semana seguinte (D-7), reduzindo chamados de suporte por quebra de sincronização.
- **Aderência aos Padrões:** 100% de cobertura via TDD, comentários XML e ausência total de exceções para regras de negócio.

### Negativas / Mitigações
- Contadores puramente em memória poderiam ser perdidos em caso de reinicialização abrupta do processo.
  - *Mitigação:* Sincronização periódica de snapshots no `MasterDb` e recarregamento dos contadores persistidos no startup da aplicação.
